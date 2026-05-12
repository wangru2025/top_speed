using System;
using System.Diagnostics;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;
using TopSpeed.Input;
using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private bool EnsureCaptureInitialized()
        {
            var desiredDeviceName = _settings.VoiceInputDeviceName ?? string.Empty;
            var captureDeviceKey = string.IsNullOrWhiteSpace(desiredDeviceName) ? string.Empty : desiredDeviceName;
            if (_captureDevice != null && string.Equals(_captureDeviceName, captureDeviceKey, StringComparison.Ordinal))
                return true;

            DisposeCapture();

            try
            {
                _captureEngine = new MiniAudioEngine();
                _captureEngine.UpdateAudioDevicesInfo();

                DeviceInfo? chosenDevice = null;
                if (!string.IsNullOrWhiteSpace(desiredDeviceName))
                {
                    for (var i = 0; i < _captureEngine.CaptureDevices.Length; i++)
                    {
                        var info = _captureEngine.CaptureDevices[i];
                        if (string.Equals(info.Name, desiredDeviceName, StringComparison.Ordinal))
                        {
                            chosenDevice = info;
                            break;
                        }
                    }
                }

                var chosenFormat = ResolveCaptureFormat(chosenDevice);
                if (TryStartCaptureDevice(chosenDevice, chosenFormat, captureDeviceKey))
                    return true;

                if (chosenDevice != null)
                {
                    var fallbackFormat = ResolveCaptureFormat(null);
                    if (TryStartCaptureDevice(null, fallbackFormat, captureDeviceKey))
                        return true;
                }

                DisposeCapture();
                return false;
            }
            catch
            {
                DisposeCapture();
                return false;
            }
        }

        private bool TryStartCaptureDevice(DeviceInfo? selectedDevice, AudioFormat format, string captureDeviceName)
        {
            SoundFlow.Abstracts.Devices.AudioCaptureDevice? captureDevice = null;
            try
            {
                captureDevice = _captureEngine!.InitializeCaptureDevice(selectedDevice, format);

                _captureChannels = Math.Max(1, captureDevice.Format.Channels);
                _captureInputSampleRate = Math.Max(1, captureDevice.Format.SampleRate);
                _captureMeasuredInputSampleRate = _captureInputSampleRate;
                ResetCaptureResamplerState();

                captureDevice.OnAudioProcessed += OnCapturedAudio;
                captureDevice.Start();

                _captureDevice = captureDevice;
                _captureDeviceName = captureDeviceName;
                return true;
            }
            catch
            {
                try
                {
                    captureDevice?.OnAudioProcessed -= OnCapturedAudio;
                }
                catch
                {
                }

                try
                {
                    captureDevice?.Stop();
                }
                catch
                {
                }

                try
                {
                    captureDevice?.Dispose();
                }
                catch
                {
                }

                _captureDevice = null;
                _captureChannels = 1;
                _captureInputSampleRate = ProtocolConstants.VoiceSampleRate;
                _captureMeasuredInputSampleRate = ProtocolConstants.VoiceSampleRate;
                _captureDeviceName = string.Empty;
                ResetCaptureResamplerState();
                return false;
            }
        }

        private void DisposeCapture()
        {
            try
            {
                if (_captureDevice != null)
                    _captureDevice.OnAudioProcessed -= OnCapturedAudio;
            }
            catch
            {
            }

            try
            {
                _captureDevice?.Stop();
            }
            catch
            {
            }

            try
            {
                _captureDevice?.Dispose();
            }
            catch
            {
            }
            _captureDevice = null;

            try
            {
                _captureEngine?.Dispose();
            }
            catch
            {
            }

            _captureEngine = null;
            _captureDeviceName = string.Empty;
            _captureChannels = 1;
            _captureInputSampleRate = ProtocolConstants.VoiceSampleRate;
            _captureMeasuredInputSampleRate = ProtocolConstants.VoiceSampleRate;
            ResetCaptureResamplerState();
            DiscardCapturedSamples();
        }

        private AudioFormat ResolveCaptureFormat(DeviceInfo? preferredDevice)
        {
            if (TryResolveCaptureFormat(preferredDevice, out var sampleRate, out var channels))
            {
                return new AudioFormat
                {
                    SampleRate = sampleRate,
                    Channels = channels,
                    Format = SampleFormat.F32,
                    Layout = AudioFormat.GetLayoutFromChannels(channels)
                };
            }

            if (_captureEngine != null && _captureEngine.CaptureDevices.Length > 0)
            {
                for (var i = 0; i < _captureEngine.CaptureDevices.Length; i++)
                {
                    if (_captureEngine.CaptureDevices[i].IsDefault
                        && TryResolveCaptureFormat(_captureEngine.CaptureDevices[i], out sampleRate, out channels))
                    {
                        return new AudioFormat
                        {
                            SampleRate = sampleRate,
                            Channels = channels,
                            Format = SampleFormat.F32,
                            Layout = AudioFormat.GetLayoutFromChannels(channels)
                        };
                    }
                }

                if (TryResolveCaptureFormat(_captureEngine.CaptureDevices[0], out sampleRate, out channels))
                {
                    return new AudioFormat
                    {
                        SampleRate = sampleRate,
                        Channels = channels,
                        Format = SampleFormat.F32,
                        Layout = AudioFormat.GetLayoutFromChannels(channels)
                    };
                }
            }

            return new AudioFormat
            {
                SampleRate = ProtocolConstants.VoiceSampleRate,
                Channels = 1,
                Format = SampleFormat.F32,
                Layout = AudioFormat.GetLayoutFromChannels(1)
            };
        }

        private static bool TryResolveCaptureFormat(DeviceInfo? device, out int sampleRate, out int channels)
        {
            sampleRate = ProtocolConstants.VoiceSampleRate;
            channels = 1;
            if (!device.HasValue)
                return false;

            var supported = device.Value.SupportedDataFormats;
            if (supported == null || supported.Length == 0)
                return false;

            var bestScore = int.MaxValue;
            var bestRateDiff = int.MaxValue;
            uint bestRate = 0;
            uint bestChannels = 0;

            for (var i = 0; i < supported.Length; i++)
            {
                var format = supported[i];
                if (format.SampleRate == 0 || format.Channels == 0)
                    continue;

                var candidateChannels = Math.Clamp(format.Channels, 1u, 2u);
                var rateScore = ScoreCaptureSampleRate(format.SampleRate);
                var rateDiff = (int)Math.Abs((long)format.SampleRate - ProtocolConstants.VoiceSampleRate);

                if (rateScore > bestScore)
                    continue;

                if (rateScore == bestScore && rateDiff > bestRateDiff)
                    continue;

                if (rateScore == bestScore && rateDiff == bestRateDiff && candidateChannels < bestChannels)
                    continue;

                bestScore = rateScore;
                bestRateDiff = rateDiff;
                bestRate = format.SampleRate;
                bestChannels = candidateChannels;
            }

            if (bestRate == 0 || bestChannels == 0)
                return false;

            sampleRate = (int)bestRate;
            channels = (int)bestChannels;
            return true;
        }

        private static int ScoreCaptureSampleRate(uint sampleRate)
        {
            if (sampleRate == ProtocolConstants.VoiceSampleRate)
                return 0;

            if (sampleRate == ProtocolConstants.VoiceSampleRate * 2)
                return 1;

            if (sampleRate % ProtocolConstants.VoiceSampleRate == 0)
                return 2;

            if (sampleRate < ProtocolConstants.VoiceSampleRate && ProtocolConstants.VoiceSampleRate % sampleRate == 0)
                return 3;

            return 4;
        }

        private static int SelectDominantChannel(Span<float> samples, int channels, int frameCount)
        {
            if (channels <= 1)
                return 0;

            Span<float> channelEnergy = channels <= 8 ? stackalloc float[8] : new float[channels];
            for (var frame = 0; frame < frameCount; frame++)
            {
                var baseIndex = frame * channels;
                for (var ch = 0; ch < channels; ch++)
                {
                    var sample = samples[baseIndex + ch];
                    channelEnergy[ch] += sample * sample;
                }
            }

            var dominantChannel = 0;
            var dominantEnergy = channelEnergy[0];
            for (var ch = 1; ch < channels; ch++)
            {
                if (channelEnergy[ch] <= dominantEnergy)
                    continue;

                dominantChannel = ch;
                dominantEnergy = channelEnergy[ch];
            }

            return dominantChannel;
        }

        private void ResetCaptureResamplerState()
        {
            _captureSourceFrameIndex = 0;
            _captureNextOutputSourceFrame = 0d;
            _captureHasPreviousSample = false;
            _capturePreviousSample = 0f;
            _captureRateMeasureStartTimestamp = 0;
            _captureRateMeasureFrameCount = 0;
        }

        private void UpdateMeasuredCaptureSampleRate(int sourceFrames)
        {
            if (sourceFrames <= 0)
                return;

            var now = Stopwatch.GetTimestamp();
            if (_captureRateMeasureStartTimestamp == 0)
            {
                _captureRateMeasureStartTimestamp = now;
                _captureRateMeasureFrameCount = 0;
            }

            _captureRateMeasureFrameCount += sourceFrames;
            var elapsedTicks = now - _captureRateMeasureStartTimestamp;
            if (elapsedTicks < Stopwatch.Frequency / 4)
                return;

            var measured = (int)Math.Round(_captureRateMeasureFrameCount * (double)Stopwatch.Frequency / elapsedTicks);
            measured = Math.Clamp(measured, 8000, 192000);
            if (measured > 0)
            {
                var current = _captureMeasuredInputSampleRate;
                if (current <= 0)
                {
                    _captureMeasuredInputSampleRate = measured;
                }
                else
                {
                    var drift = Math.Abs(measured - current) / (double)current;
                    _captureMeasuredInputSampleRate = drift >= 0.2
                        ? measured
                        : (int)Math.Round((current * 0.8) + (measured * 0.2));
                }
            }

            _captureRateMeasureStartTimestamp = now;
            _captureRateMeasureFrameCount = 0;
        }

        private void AppendResampledCapturedFrames(Span<float> samples, int channels, int frameCount, int dominantChannel, float gain)
        {
            var sourceRate = _captureMeasuredInputSampleRate > 0
                ? _captureMeasuredInputSampleRate
                : (_captureInputSampleRate <= 0 ? ProtocolConstants.VoiceSampleRate : _captureInputSampleRate);
            var targetRate = ProtocolConstants.VoiceSampleRate;
            if (sourceRate == targetRate)
            {
                for (var frame = 0; frame < frameCount; frame++)
                {
                    var sample = samples[frame * channels + dominantChannel];
                    PushCapturedSample(sample * gain);
                }

                return;
            }

            var sourceFramesPerOutputSample = (double)sourceRate / targetRate;
            for (var frame = 0; frame < frameCount; frame++)
            {
                var currentSample = samples[frame * channels + dominantChannel];
                var currentFrameIndex = _captureSourceFrameIndex;
                _captureSourceFrameIndex++;

                if (!_captureHasPreviousSample)
                {
                    _capturePreviousSample = currentSample;
                    _captureHasPreviousSample = true;
                    continue;
                }

                var previousFrameIndex = currentFrameIndex - 1;
                while (_captureNextOutputSourceFrame <= currentFrameIndex)
                {
                    var alpha = _captureNextOutputSourceFrame - previousFrameIndex;
                    if (alpha < 0d)
                        alpha = 0d;
                    else if (alpha > 1d)
                        alpha = 1d;

                    var interpolated = _capturePreviousSample + (currentSample - _capturePreviousSample) * (float)alpha;
                    PushCapturedSample(interpolated * gain);
                    _captureNextOutputSourceFrame += sourceFramesPerOutputSample;
                }

                _capturePreviousSample = currentSample;
            }
        }

        private void PushCapturedSample(float sample)
        {
            if (sample < -1f)
                sample = -1f;
            else if (sample > 1f)
                sample = 1f;

            _capturedSamples.Add((short)Math.Round(sample * short.MaxValue));
        }

        private void TrimCapturedSamples()
        {
            if (_capturedSamples.Count <= MaxCapturedSamples)
                return;

            var trim = _capturedSamples.Count - MaxCapturedSamples;
            _capturedSamples.RemoveRange(0, trim);
        }

        private void OnCapturedAudio(Span<float> samples, Capability _capability)
        {
            if (samples.Length == 0)
                return;

            var channels = _captureChannels <= 0 ? 1 : _captureChannels;
            var frameCount = samples.Length / channels;
            if (frameCount <= 0)
                return;

            var gain = GetCaptureInputGain();
            var dominantChannel = SelectDominantChannel(samples, channels, frameCount);

            lock (_captureLock)
            {
                UpdateMeasuredCaptureSampleRate(frameCount);
                AppendResampledCapturedFrames(samples, channels, frameCount, dominantChannel, gain);
                TrimCapturedSamples();
            }
        }

        private float GetCaptureInputGain()
        {
            var percent = _settings.VoiceInputGainPercent;
            if (percent < DriveSettings.MinVoiceInputGainPercent)
                percent = DriveSettings.MinVoiceInputGainPercent;
            else if (percent > DriveSettings.MaxVoiceInputGainPercent)
                percent = DriveSettings.MaxVoiceInputGainPercent;
            return percent / 100f;
        }

        private bool TryReadCapturedFrame(short[] target)
        {
            if (target == null || target.Length == 0)
                return false;

            lock (_captureLock)
            {
                if (_capturedSamples.Count < target.Length)
                    return false;

                _capturedSamples.CopyTo(0, target, 0, target.Length);
                _capturedSamples.RemoveRange(0, target.Length);
                return true;
            }
        }

        private void DiscardCapturedSamples()
        {
            lock (_captureLock)
                _capturedSamples.Clear();
        }
    }
}
