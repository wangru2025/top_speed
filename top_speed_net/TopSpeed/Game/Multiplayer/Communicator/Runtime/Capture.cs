using System;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;
using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private bool EnsureCaptureInitialized()
        {
            var desiredDeviceName = _settings.VoiceInputDeviceName ?? string.Empty;
            if (_captureDevice != null && string.Equals(_captureDeviceName, desiredDeviceName, StringComparison.Ordinal))
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

                var format = new AudioFormat
                {
                    SampleRate = ProtocolConstants.VoiceSampleRate,
                    Channels = 1,
                    Format = SampleFormat.F32,
                    Layout = AudioFormat.GetLayoutFromChannels(1)
                };

                if (TryStartCaptureDevice(chosenDevice, format, desiredDeviceName))
                    return true;

                if (chosenDevice != null && TryStartCaptureDevice(null, format, desiredDeviceName))
                    return true;

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
                captureDevice.OnAudioProcessed += OnCapturedAudio;
                captureDevice.Start();

                _captureDevice = captureDevice;
                _captureChannels = Math.Max(1, captureDevice.Format.Channels);
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
                _captureDeviceName = string.Empty;
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
            DiscardCapturedSamples();
        }

        private void OnCapturedAudio(Span<float> samples, Capability _capability)
        {
            if (samples.Length == 0)
                return;

            var channels = _captureChannels <= 0 ? 1 : _captureChannels;
            var frameCount = samples.Length / channels;
            if (frameCount <= 0)
                return;

            var sumSquares = 0d;
            lock (_captureLock)
            {
                for (var frame = 0; frame < frameCount; frame++)
                {
                    var baseIndex = frame * channels;
                    var mixed = 0f;
                    for (var ch = 0; ch < channels; ch++)
                        mixed += samples[baseIndex + ch];
                    mixed /= channels;

                    if (mixed < -1f)
                        mixed = -1f;
                    else if (mixed > 1f)
                        mixed = 1f;

                    sumSquares += mixed * mixed;
                    _capturedSamples.Add((short)Math.Round(mixed * short.MaxValue));
                }

                if (_capturedSamples.Count > MaxCapturedSamples)
                {
                    var trim = _capturedSamples.Count - MaxCapturedSamples;
                    _capturedSamples.RemoveRange(0, trim);
                }
            }

            var rms = Math.Sqrt(sumSquares / frameCount);
            if (rms >= VoiceActivationThreshold)
                _lastVoiceActivityUtcTicks = DateTime.UtcNow.Ticks;
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

        private void ResetVoiceActivity()
        {
            _lastVoiceActivityUtcTicks = 0;
        }
    }
}
