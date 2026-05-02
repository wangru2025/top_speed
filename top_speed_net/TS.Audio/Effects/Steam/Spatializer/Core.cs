using System;
using System.Numerics;
using SoundFlow.Abstracts;
using SteamAudio;

namespace TS.Audio
{
    internal sealed unsafe partial class SteamAudioSpatialModifier : SoundModifier, IDisposable
    {
        private readonly SteamAudioRuntime _runtime;
        private readonly object _sync;
        private readonly bool _useBinaural;
        private readonly HrtfDownmixMode _downmixMode;
        private readonly int _outputChannels;
        private readonly Action<Exception>? _onProcessingException;

        private IPL.DirectEffect _directEffect;
        private IPL.BinauralEffect _binauralEffect;
        private IPL.ReflectionEffect _reflectionEffect;
        private IPL.AudioBuffer _inputBuffer;
        private IPL.AudioBuffer _directBuffer;
        private IPL.AudioBuffer _outputBuffer;
        private IPL.AudioBuffer _reflectionInputBuffer;
        private IPL.AudioBuffer _reflectionOutputBuffer;
        private float[] _processingScratch = Array.Empty<float>();
        private bool _initialized;
        private bool _disposed;

        private Vector3 _localPosition;
        private IPL.Vector3 _direction;
        private float _distanceAttenuation = 1f;
        private readonly float[] _baseAirAbsorption = [1f, 1f, 1f];
        private float _baseOcclusion = 1f;
        private float _spatialBlend = 1f;
        private bool _stereoWidening;

        private bool _hasDirectSimulation;
        private readonly float[] _simulationAirAbsorption = [1f, 1f, 1f];
        private readonly float[] _simulationTransmission = [1f, 1f, 1f];
        private float _simulationOcclusion = 1f;

        private bool _hasReverbSimulation;
        private readonly float[] _reverbTimes = new float[3];
        private readonly float[] _reverbEq = [1f, 1f, 1f];
        private int _reverbDelay;
        private float _reverbWetTarget;
        private float _reverbWetCurrent;

        public SteamAudioSpatialModifier(SteamAudioRuntime runtime, int outputChannels, bool useBinaural, HrtfDownmixMode downmixMode, Action<Exception>? onProcessingException = null)
        {
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _outputChannels = outputChannels > 0 ? outputChannels : 2;
            _useBinaural = useBinaural && _runtime.HrtfAvailable && _outputChannels == 2;
            _downmixMode = downmixMode;
            _onProcessingException = onProcessingException;
            _sync = new object();
            Initialize();
        }

        public bool UsesSteamAudio => _initialized;
        public bool IsBinauralActive => _initialized && _useBinaural;

        public override float ProcessSample(float sample, int channel)
        {
            return sample;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _initialized = false;
                ReleaseNativeResources();
                _processingScratch = Array.Empty<float>();
            }
        }

        public void Reset()
        {
            if (_disposed || !_initialized)
                return;

            lock (_sync)
            {
                if (_disposed || !_initialized)
                    return;

                IPL.DirectEffectReset(_directEffect);
                if (_binauralEffect.Handle != IntPtr.Zero)
                    IPL.BinauralEffectReset(_binauralEffect);
                if (_reflectionEffect.Handle != IntPtr.Zero)
                    IPL.ReflectionEffectReset(_reflectionEffect);
                _reverbWetCurrent = 0f;
            }
        }

        private void Initialize()
        {
            var initialized = false;
            try
            {
                var audioSettings = _runtime.AudioSettings;
                var directSettings = new IPL.DirectEffectSettings
                {
                    NumChannels = _useBinaural ? 1 : _outputChannels
                };

                if (IPL.DirectEffectCreate(_runtime.Context, in audioSettings, in directSettings, out _directEffect) != IPL.Error.Success || _directEffect.Handle == IntPtr.Zero)
                    return;

                if (IPL.AudioBufferAllocate(_runtime.Context, directSettings.NumChannels, _runtime.AudioSettings.FrameSize, ref _inputBuffer) != IPL.Error.Success)
                    return;
                if (IPL.AudioBufferAllocate(_runtime.Context, directSettings.NumChannels, _runtime.AudioSettings.FrameSize, ref _directBuffer) != IPL.Error.Success)
                    return;

                if (_useBinaural)
                {
                    var effectSettings = new IPL.BinauralEffectSettings
                    {
                        Hrtf = _runtime.Hrtf
                    };

                    if (IPL.BinauralEffectCreate(_runtime.Context, in audioSettings, in effectSettings, out _binauralEffect) != IPL.Error.Success || _binauralEffect.Handle == IntPtr.Zero)
                        return;

                    if (IPL.AudioBufferAllocate(_runtime.Context, 2, _runtime.AudioSettings.FrameSize, ref _outputBuffer) != IPL.Error.Success)
                        return;
                }
                else if (IPL.AudioBufferAllocate(_runtime.Context, _outputChannels, _runtime.AudioSettings.FrameSize, ref _outputBuffer) != IPL.Error.Success)
                {
                    return;
                }

                if (_runtime.SupportsReflections)
                {
                    var reflectionSettings = new IPL.ReflectionEffectSettings
                    {
                        Type = _runtime.ReflectionType,
                        NumChannels = 1,
                        IrSize = 1
                    };

                    if (IPL.ReflectionEffectCreate(_runtime.Context, in audioSettings, in reflectionSettings, out _reflectionEffect) == IPL.Error.Success && _reflectionEffect.Handle != IntPtr.Zero)
                    {
                        if (IPL.AudioBufferAllocate(_runtime.Context, 1, _runtime.AudioSettings.FrameSize, ref _reflectionInputBuffer) != IPL.Error.Success)
                            return;
                        if (IPL.AudioBufferAllocate(_runtime.Context, 1, _runtime.AudioSettings.FrameSize, ref _reflectionOutputBuffer) != IPL.Error.Success)
                            return;
                    }
                }

                _initialized = _inputBuffer.Data != IntPtr.Zero
                    && _directBuffer.Data != IntPtr.Zero
                    && _outputBuffer.Data != IntPtr.Zero;
                initialized = _initialized;
            }
            catch
            {
                _initialized = false;
            }
            finally
            {
                if (!initialized)
                {
                    _initialized = false;
                    ReleaseNativeResources();
                }
            }
        }

        private void ReleaseNativeResources()
        {
            if (_reflectionInputBuffer.Data != IntPtr.Zero)
                IPL.AudioBufferFree(_runtime.Context, ref _reflectionInputBuffer);
            if (_reflectionOutputBuffer.Data != IntPtr.Zero)
                IPL.AudioBufferFree(_runtime.Context, ref _reflectionOutputBuffer);
            if (_inputBuffer.Data != IntPtr.Zero)
                IPL.AudioBufferFree(_runtime.Context, ref _inputBuffer);
            if (_directBuffer.Data != IntPtr.Zero)
                IPL.AudioBufferFree(_runtime.Context, ref _directBuffer);
            if (_outputBuffer.Data != IntPtr.Zero)
                IPL.AudioBufferFree(_runtime.Context, ref _outputBuffer);
            if (_reflectionEffect.Handle != IntPtr.Zero)
                IPL.ReflectionEffectRelease(ref _reflectionEffect);
            if (_binauralEffect.Handle != IntPtr.Zero)
                IPL.BinauralEffectRelease(ref _binauralEffect);
            if (_directEffect.Handle != IntPtr.Zero)
                IPL.DirectEffectRelease(ref _directEffect);
        }

        private void HandleProcessingException(Exception exception)
        {
            lock (_sync)
            {
                _initialized = false;
                Enabled = false;
            }

            _onProcessingException?.Invoke(exception);
        }

        private static IPL.Vector3 ToDirection(Vector3 localPosition)
        {
            var lengthSquared = localPosition.LengthSquared();
            if (lengthSquared <= 1e-6f)
                return new IPL.Vector3(0f, 0f, -1f);

            var normalized = Vector3.Normalize(localPosition);
            return new IPL.Vector3(normalized.X, normalized.Y, normalized.Z);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
