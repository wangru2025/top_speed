using System;
using SteamAudio;

namespace TS.Audio
{
    internal sealed unsafe partial class SteamAudioSpatialModifier
    {
        public override void Process(Span<float> buffer, int channels)
        {
            try
            {
                if (!Enabled || _disposed || !_initialized || buffer.IsEmpty)
                    return;

                var frames = buffer.Length / Math.Max(1, channels);
                if (frames <= 0 || channels != _outputChannels)
                    return;

                var frameSize = _runtime.AudioSettings.FrameSize;
                if (frameSize <= 0)
                    return;

                EnsureProcessingScratch(frameSize * channels);
                var frameOffset = 0;
                while (frameOffset < frames)
                {
                    var chunkFrames = Math.Min(frameSize, frames - frameOffset);
                    var chunkSamples = chunkFrames * channels;
                    var scratchSamples = frameSize * channels;
                    var sourceSlice = buffer.Slice(frameOffset * channels, chunkSamples);
                    var scratch = _processingScratch.AsSpan(0, scratchSamples);
                    sourceSlice.CopyTo(scratch);
                    if (chunkSamples < scratchSamples)
                        scratch.Slice(chunkSamples).Clear();

                    lock (_sync)
                    {
                        if (_disposed || !_initialized || !HasRequiredBuffers())
                            return;

                        if (_useBinaural)
                            ProcessBinaural(scratch, channels, frameSize);
                        else
                            ProcessDirect(scratch, channels, frameSize);
                    }

                    scratch.Slice(0, chunkSamples).CopyTo(sourceSlice);
                    frameOffset += chunkFrames;
                }
            }
            catch (Exception ex)
            {
                buffer.Clear();
                HandleProcessingException(ex);
            }
        }

        private void EnsureProcessingScratch(int requiredSamples)
        {
            if (_processingScratch.Length < requiredSamples)
                _processingScratch = new float[requiredSamples];
        }

        private void ProcessDirect(Span<float> buffer, int channels, int frames)
        {
            if (_directEffect.Handle == IntPtr.Zero || _inputBuffer.Data == IntPtr.Zero || _outputBuffer.Data == IntPtr.Zero)
                return;

            if (ShouldRenderReverb())
                WriteDownmixedMono(buffer, channels, frames, _reflectionInputBuffer);

            WriteInterleavedToBuffer(buffer, channels, frames, _inputBuffer);

            var parameters = CreateDirectEffectParameters();
            IPL.DirectEffectApply(_directEffect, ref parameters, ref _inputBuffer, ref _outputBuffer);
            ReadBufferToInterleaved(_outputBuffer, buffer, channels, frames);
            ApplyStereoWidening(buffer, channels, frames);
            ApplyReflections(buffer, channels, frames);
        }

        private void ProcessBinaural(Span<float> buffer, int channels, int frames)
        {
            if (_directEffect.Handle == IntPtr.Zero
                || _binauralEffect.Handle == IntPtr.Zero
                || _inputBuffer.Data == IntPtr.Zero
                || _directBuffer.Data == IntPtr.Zero
                || _outputBuffer.Data == IntPtr.Zero
                || !_runtime.HrtfAvailable
                || _runtime.Hrtf.Handle == IntPtr.Zero)
            {
                return;
            }

            WriteDownmixedMono(buffer, channels, frames, _inputBuffer);
            if (ShouldRenderReverb())
                CopyBuffer(_inputBuffer, _reflectionInputBuffer, frames);

            var directParameters = CreateDirectEffectParameters();
            IPL.DirectEffectApply(_directEffect, ref directParameters, ref _inputBuffer, ref _directBuffer);

            var binauralParameters = new IPL.BinauralEffectParams
            {
                Direction = _direction,
                Interpolation = IPL.HrtfInterpolation.Bilinear,
                SpatialBlend = _spatialBlend,
                Hrtf = _runtime.Hrtf,
                PeakDelays = IntPtr.Zero
            };

            IPL.BinauralEffectApply(_binauralEffect, ref binauralParameters, ref _directBuffer, ref _outputBuffer);
            ReadBufferToInterleaved(_outputBuffer, buffer, channels, frames);
            ApplyStereoWidening(buffer, channels, frames);
            ApplyReflections(buffer, channels, frames);
        }

        private IPL.DirectEffectParams CreateDirectEffectParameters()
        {
            var occlusion = _hasDirectSimulation ? _simulationOcclusion : _baseOcclusion;
            var airLow = _hasDirectSimulation ? _simulationAirAbsorption[0] : _baseAirAbsorption[0];
            var airMid = _hasDirectSimulation ? _simulationAirAbsorption[1] : _baseAirAbsorption[1];
            var airHigh = _hasDirectSimulation ? _simulationAirAbsorption[2] : _baseAirAbsorption[2];
            var transLow = _hasDirectSimulation ? _simulationTransmission[0] : 1f;
            var transMid = _hasDirectSimulation ? _simulationTransmission[1] : 1f;
            var transHigh = _hasDirectSimulation ? _simulationTransmission[2] : 1f;

            var parameters = new IPL.DirectEffectParams
            {
                Flags = IPL.DirectEffectFlags.ApplyDistanceAttenuation | IPL.DirectEffectFlags.ApplyAirAbsorption,
                TransmissionType = IPL.TransmissionType.FrequencyDependent,
                DistanceAttenuation = _distanceAttenuation,
                Directivity = 1f,
                Occlusion = Clamp01(occlusion)
            };

            parameters.AirAbsorption[0] = Clamp01(airLow);
            parameters.AirAbsorption[1] = Clamp01(airMid);
            parameters.AirAbsorption[2] = Clamp01(airHigh);
            parameters.Transmission[0] = Clamp01(transLow);
            parameters.Transmission[1] = Clamp01(transMid);
            parameters.Transmission[2] = Clamp01(transHigh);

            if (parameters.Occlusion < 0.999f)
                parameters.Flags |= IPL.DirectEffectFlags.ApplyOcclusion;

            if (parameters.Transmission[0] < 0.999f || parameters.Transmission[1] < 0.999f || parameters.Transmission[2] < 0.999f)
                parameters.Flags |= IPL.DirectEffectFlags.ApplyTransmission;

            return parameters;
        }

        private void ApplyReflections(Span<float> buffer, int channels, int frames)
        {
            if (_reflectionEffect.Handle == IntPtr.Zero)
                return;

            if (_hasReverbSimulation)
                _reverbWetCurrent += (_reverbWetTarget - _reverbWetCurrent) * 0.05f;
            else
                _reverbWetCurrent *= 0.9f;

            if (_reverbWetCurrent <= 0.0001f)
                return;

            var reflectionParams = new IPL.ReflectionEffectParams
            {
                Type = _runtime.ReflectionType,
                NumChannels = 1,
                IrSize = 0,
                Ir = default,
                Delay = _reverbDelay
            };

            reflectionParams.ReverbTimes[0] = Math.Max(0f, _reverbTimes[0]);
            reflectionParams.ReverbTimes[1] = Math.Max(0f, _reverbTimes[1]);
            reflectionParams.ReverbTimes[2] = Math.Max(0f, _reverbTimes[2]);
            reflectionParams.Eq[0] = Clamp01(_reverbEq[0]);
            reflectionParams.Eq[1] = Clamp01(_reverbEq[1]);
            reflectionParams.Eq[2] = Clamp01(_reverbEq[2]);

            IPL.ReflectionEffectApply(_reflectionEffect, ref reflectionParams, ref _reflectionInputBuffer, ref _reflectionOutputBuffer, default);

            var wet = GetChannelPointer(_reflectionOutputBuffer, 0);
            for (var frame = 0; frame < frames; frame++)
            {
                var wetSample = wet[frame] * _reverbWetCurrent;
                var cursor = frame * channels;
                for (var channel = 0; channel < channels; channel++)
                    buffer[cursor + channel] += wetSample;
            }
        }

        private void ApplyStereoWidening(Span<float> buffer, int channels, int frames)
        {
            if (!_stereoWidening || channels < 2)
                return;

            GetStereoWideningGains(_direction, out var leftGain, out var rightGain);
            for (var frame = 0; frame < frames; frame++)
            {
                var index = frame * channels;
                buffer[index] *= leftGain;
                buffer[index + 1] *= rightGain;
            }
        }

        private bool ShouldRenderReverb()
        {
            return _reflectionEffect.Handle != IntPtr.Zero
                && _reflectionInputBuffer.Data != IntPtr.Zero
                && _reflectionOutputBuffer.Data != IntPtr.Zero
                && (_hasReverbSimulation || _reverbWetCurrent > 0.0001f);
        }

        private bool HasRequiredBuffers()
        {
            return _directEffect.Handle != IntPtr.Zero
                && _inputBuffer.Data != IntPtr.Zero
                && _outputBuffer.Data != IntPtr.Zero
                && (!_useBinaural || (_binauralEffect.Handle != IntPtr.Zero && _directBuffer.Data != IntPtr.Zero));
        }

        private float DownmixFrame(Span<float> source, int offset, int channels)
        {
            if (channels <= 1)
                return source[offset];

            return _downmixMode switch
            {
                HrtfDownmixMode.Left => source[offset],
                HrtfDownmixMode.Right => source[offset + 1],
                HrtfDownmixMode.Sum => Clamp(source[offset] + source[offset + 1], -1f, 1f),
                _ => (source[offset] + source[offset + 1]) * 0.5f
            };
        }
    }
}
