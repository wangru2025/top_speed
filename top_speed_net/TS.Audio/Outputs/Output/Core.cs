using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Threading;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Enums;
using SoundFlow.Structs;
using SoundFlow.Structs.Events;
using SfAudioEngine = SoundFlow.Abstracts.AudioEngine;
using SfAudioFormat = SoundFlow.Structs.AudioFormat;

namespace TS.Audio
{
    public sealed partial class AudioOutput : IDisposable
    {
        private readonly AudioOutputConfig _config;
        private readonly AudioSystemConfig _systemConfig;
        private readonly AudioDiagnostics _diagnostics;
        private readonly SfAudioEngine _backendEngine;
        private readonly AudioPlaybackDevice _playbackDevice;
        private readonly LimiterModifier _limiterModifier;
        private readonly List<AudioSourceHandle> _sources;
        private readonly List<TrackStream> _streams;
        private readonly List<AudioSourceHandle> _sourceUpdateSnapshot;
        private readonly List<TrackStream> _streamUpdateSnapshot;
        private readonly Dictionary<string, AudioBus> _buses;
        private readonly object _sourceLock;
        private readonly object _busLock;
        private readonly ManualResetEventSlim _firstRenderObserved;
        private SteamAudioRuntime? _steamAudioRuntime;
        private RoomAcoustics _roomAcoustics;
        private readonly AudioBus _mainBus;
        private ListenerStateSnapshot _listenerState;
        private volatile int _actualFrameSize;
        private volatile float _masterVolume = 1f;
        private volatile float _lastPreLimiterPeak;
        private volatile float _lastPostLimiterPeak;
        private volatile float _lastLimiterGain = 1f;
        private int _listenerApplyQueued;
        private bool _disposed;

        public AudioOutput(SfAudioEngine backendEngine, AudioOutputConfig config, AudioSystemConfig systemConfig, AudioDiagnostics diagnostics)
        {
            _backendEngine = backendEngine ?? throw new ArgumentNullException(nameof(backendEngine));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _systemConfig = systemConfig ?? throw new ArgumentNullException(nameof(systemConfig));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _sources = new List<AudioSourceHandle>();
            _streams = new List<TrackStream>();
            _sourceUpdateSnapshot = new List<AudioSourceHandle>();
            _streamUpdateSnapshot = new List<TrackStream>();
            _lifecycleQueue = new System.Collections.Concurrent.ConcurrentQueue<AudioLifecycleWork>();
            _deferredLifecycle = new List<AudioLifecycleWork>();
            _controlQueue = new System.Collections.Concurrent.ConcurrentQueue<AudioControlWork>();
            _buses = new Dictionary<string, AudioBus>(StringComparer.OrdinalIgnoreCase);
            _sourceLock = new object();
            _busLock = new object();
            _firstRenderObserved = new ManualResetEventSlim(false);
            _roomAcoustics = RoomAcoustics.Default;
            _listenerState = ListenerStateSnapshot.Default;

            var device = ResolvePlaybackDevice(_config);
            var resolvedFormat = ResolveFormat(_config, _systemConfig, device);
            SoundFlowFormat = resolvedFormat;
            _config.SampleRate = (uint)resolvedFormat.SampleRate;
            _config.Channels = (uint)resolvedFormat.Channels;
            if (_config.PeriodSizeInFrames == 0)
                _config.PeriodSizeInFrames = _systemConfig.PeriodSizeInFrames > 0 ? _systemConfig.PeriodSizeInFrames : 256;

            var deviceConfig = new MiniAudioDeviceConfig
            {
                PeriodSizeInFrames = _config.PeriodSizeInFrames
            };
            _playbackDevice = _backendEngine.InitializePlaybackDevice(device, resolvedFormat, deviceConfig);
            _playbackDevice.MasterMixer.Volume = 1f;
            _limiterModifier = new LimiterModifier(GetMasterVolume, UpdateLimiterMeters);
            _playbackDevice.MasterMixer.AddModifier(_limiterModifier);
            _actualFrameSize = (int)_config.PeriodSizeInFrames;
            _backendEngine.AudioFramesRendered += OnAudioFramesRendered;
            _playbackDevice.Start();
            _firstRenderObserved.Wait(100);
            _steamAudioRuntime = SteamAudioRuntime.TryCreate(_systemConfig, resolvedFormat.SampleRate, resolvedFormat.Channels, _actualFrameSize);

            _mainBus = CreateBusInternal("main", null, null);
        }

        public string Name => _config.Name;
        public int SampleRate => SoundFlowFormat.SampleRate;
        public int Channels => SoundFlowFormat.Channels;
        public uint PeriodSizeInFrames => _config.PeriodSizeInFrames;
        public bool IsHrtfActive => _steamAudioRuntime?.HrtfAvailable == true;
        public AudioBus MainBus => _mainBus;
        internal AudioDiagnostics Diagnostics => _diagnostics;
        internal SfAudioEngine BackendEngine => _backendEngine;
        internal AudioPlaybackDevice PlaybackDevice => _playbackDevice;
        internal SfAudioFormat SoundFlowFormat { get; }
        internal AudioSystemConfig SystemConfig => _systemConfig;
        internal Vector3 ListenerPosition => _listenerState.Position;
        internal Vector3 ListenerVelocity => _listenerState.Velocity;
        internal Vector3 ListenerForward => _listenerState.Forward;
        internal Vector3 ListenerUp => _listenerState.Up;
        internal SteamAudioRuntime? SteamAudioRuntime => _steamAudioRuntime;
        internal ListenerStateSnapshot CaptureListenerState() => _listenerState;

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Clamp01(volume);
            _diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.OutputMasterVolumeChanged,
                AudioDiagnosticEntityType.Output,
                Name,
                null,
                null,
                "Audio output master volume changed.",
                new Dictionary<string, object?>
                {
                    ["masterVolume"] = GetMasterVolume(),
                    ["masterVolumeDb"] = AudioMath.GainToDecibels(GetMasterVolume()),
                    ["lastPreLimiterPeak"] = _lastPreLimiterPeak,
                    ["lastPreLimiterPeakDbfs"] = AudioMath.GainToDecibels(_lastPreLimiterPeak),
                    ["lastPostLimiterPeak"] = _lastPostLimiterPeak,
                    ["lastPostLimiterPeakDbfs"] = AudioMath.GainToDecibels(_lastPostLimiterPeak)
                });
        }

        public float GetMasterVolume()
        {
            return _masterVolume;
        }

        public AudioBus CreateBus(string name)
        {
            return CreateBus(name, _mainBus);
        }

        public AudioBus CreateBus(string name, AudioBus? parent)
        {
            return CreateBus(name, parent, null);
        }

        internal AudioBus CreateBus(string name, AudioBus? parent, PlaybackPolicy? defaults)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bus name is required.", nameof(name));

            lock (_busLock)
            {
                if (_buses.TryGetValue(name, out var existing))
                    return existing;

                return CreateBusInternal(name, parent ?? _mainBus, defaults);
            }
        }

        public AudioBus GetBus(string name)
        {
            lock (_busLock)
            {
                if (_buses.TryGetValue(name, out var bus))
                    return bus;
            }

            throw new KeyNotFoundException("Audio bus not found: " + name);
        }

        public AudioOutputSnapshot CaptureSnapshot()
        {
            AudioSourceHandle[] sourceSnapshot;
            TrackStream[] streamSnapshot;

            lock (_sourceLock)
            {
                sourceSnapshot = CaptureSourceSnapshotUnsafe();
                streamSnapshot = _streams.ToArray();
            }

            AudioBus[] busSnapshot;
            lock (_busLock)
                busSnapshot = _buses.Values.ToArray();

            var buses = new List<AudioBusSnapshot>(busSnapshot.Length);
            for (var i = 0; i < busSnapshot.Length; i++)
                buses.Add(busSnapshot[i].CaptureSnapshot());

            var sources = new List<AudioSourceSnapshot>(sourceSnapshot.Length);
            for (var i = 0; i < sourceSnapshot.Length; i++)
            {
                if (sourceSnapshot[i].IsDisposed)
                    continue;

                sources.Add(sourceSnapshot[i].CaptureSnapshot());
            }

            var master = GetMasterVolume();
            return new AudioOutputSnapshot(
                Name,
                SampleRate,
                Channels,
                master,
                AudioMath.GainToDecibels(master),
                _lastPreLimiterPeak,
                AudioMath.GainToDecibels(_lastPreLimiterPeak),
                _lastPostLimiterPeak,
                AudioMath.GainToDecibels(_lastPostLimiterPeak),
                IsHrtfActive,
                sourceSnapshot.Length,
                streamSnapshot.Length,
                0,
                0,
                buses,
                sources);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            AudioSourceHandle[] sourceSnapshot;
            TrackStream[] streamSnapshot;
            AudioBus[] busSnapshot;
            lock (_sourceLock)
            {
                sourceSnapshot = _sources.ToArray();
                streamSnapshot = _streams.ToArray();
                _sources.Clear();
                _streams.Clear();
            }

            lock (_busLock)
            {
                busSnapshot = _buses.Values.Where(bus => !ReferenceEquals(bus, _mainBus)).ToArray();
                _buses.Clear();
            }

            for (var i = 0; i < streamSnapshot.Length; i++)
                streamSnapshot[i].Dispose();

            for (var i = 0; i < sourceSnapshot.Length; i++)
                sourceSnapshot[i].Dispose();

            DrainControl();
            DrainLifecycle();

            try
            {
                _playbackDevice.Stop();
            }
            catch
            {
            }

            DrainLifecycle(force: true);

            for (var i = 0; i < busSnapshot.Length; i++)
                busSnapshot[i].Dispose();

            _mainBus.Dispose();

            _playbackDevice.Dispose();
            _backendEngine.AudioFramesRendered -= OnAudioFramesRendered;
            _steamAudioRuntime?.Dispose();
            _firstRenderObserved.Dispose();
            _diagnostics.Emit(
                AudioDiagnosticLevel.Info,
                AudioDiagnosticKind.OutputDisposed,
                AudioDiagnosticEntityType.Output,
                Name,
                null,
                null,
                "Audio output disposed.");
        }

        private void OnAudioFramesRendered(object? sender, AudioFramesRenderedEventArgs args)
        {
            if (!ReferenceEquals(args.Device, _playbackDevice))
                return;

            if (args.FrameCount <= 0)
                return;

            _actualFrameSize = args.FrameCount;
            _firstRenderObserved.Set();
        }

        internal AudioDiagnosticMixSnapshot CaptureMixSnapshot()
        {
            AudioSourceHandle[] sourceSnapshot;
            lock (_sourceLock)
                sourceSnapshot = CaptureSourceSnapshotUnsafe();

            var activeSources = new List<AudioSourceSnapshot>(sourceSnapshot.Length);
            for (var i = 0; i < sourceSnapshot.Length; i++)
            {
                if (sourceSnapshot[i].IsDisposed)
                    continue;

                var snapshot = sourceSnapshot[i].CaptureSnapshot();
                if (snapshot.IsPlaying)
                    activeSources.Add(snapshot);
            }

            var master = GetMasterVolume();
            return new AudioDiagnosticMixSnapshot(
                Name,
                master,
                AudioMath.GainToDecibels(master),
                _lastPreLimiterPeak,
                AudioMath.GainToDecibels(_lastPreLimiterPeak),
                _lastPostLimiterPeak,
                AudioMath.GainToDecibels(_lastPostLimiterPeak),
                _lastLimiterGain,
                AudioMath.GainToDecibels(_lastLimiterGain),
                activeSources);
        }

        internal void SyncSteamAudioRuntime()
        {
            if (_disposed)
                return;

            var frameSize = _actualFrameSize;
            if (frameSize <= 0)
                return;

            if (_steamAudioRuntime is { AudioSettings.FrameSize: var currentFrameSize } && currentFrameSize == frameSize)
                return;

            var replacement = SteamAudioRuntime.TryCreate(_systemConfig, SoundFlowFormat.SampleRate, SoundFlowFormat.Channels, frameSize);
            var previous = _steamAudioRuntime;
            _steamAudioRuntime = replacement;

            AudioSourceHandle[] sourceSnapshot;
            lock (_sourceLock)
                sourceSnapshot = CaptureSourceSnapshotUnsafe();

            for (var i = 0; i < sourceSnapshot.Length; i++)
                sourceSnapshot[i].QueueRefreshSteamAudioSpatial();

            DrainLifecycle();

            if (previous != null)
                EnqueueDeferredLifecycle(previous.Dispose, "output-dispose-replaced-steam-runtime");
        }

        private AudioSourceHandle[] CaptureSourceSnapshotUnsafe()
        {
            if (_sources.Count == 0)
                return Array.Empty<AudioSourceHandle>();

            var active = new List<AudioSourceHandle>(_sources.Count);
            for (var i = 0; i < _sources.Count; i++)
            {
                var source = _sources[i];
                if (source != null && !source.IsDisposed)
                    active.Add(source);
            }

            return active.ToArray();
        }

        internal void RemoveSource(AudioSourceHandle source)
        {
            lock (_sourceLock)
                _sources.Remove(source);
        }

        internal void RemoveStream(TrackStream stream)
        {
            lock (_sourceLock)
                _streams.Remove(stream);
            _diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.StreamDisposed,
                AudioDiagnosticEntityType.Stream,
                Name,
                null,
                null,
                "Audio stream disposed.");
        }

        internal void UnregisterBus(AudioBus bus)
        {
            lock (_busLock)
                _buses.Remove(bus.Name);
        }

        private AudioBus CreateBusInternal(string name, AudioBus? parent, PlaybackPolicy? defaults)
        {
            var bus = new AudioBus(this, name, parent, defaults);
            _buses[name] = bus;
            return bus;
        }

        private SoundFlow.Structs.DeviceInfo? ResolvePlaybackDevice(AudioOutputConfig config)
        {
            _backendEngine.UpdateAudioDevicesInfo();
            if (!config.DeviceIndex.HasValue)
            {
                var devices = _backendEngine.PlaybackDevices;
                for (var i = 0; i < devices.Length; i++)
                {
                    if (devices[i].IsDefault)
                        return devices[i];
                }

                return devices.Length > 0 ? devices[0] : null;
            }

            var index = config.DeviceIndex.Value;
            if (index < 0 || index >= _backendEngine.PlaybackDevices.Length)
                throw new ArgumentOutOfRangeException(nameof(config.DeviceIndex), "Playback device index is out of range.");

            return _backendEngine.PlaybackDevices[index];
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioOutput));
        }

        private static SfAudioFormat ResolveFormat(AudioOutputConfig outputConfig, AudioSystemConfig systemConfig, DeviceInfo? device)
        {
            var nativeFormat = SelectNativeFormat(device, systemConfig.UseHrtf, (int)systemConfig.Channels);
            var sampleRate = outputConfig.SampleRate > 0
                ? (int)outputConfig.SampleRate
                : nativeFormat?.SampleRate > 0
                    ? (int)nativeFormat.Value.SampleRate
                : systemConfig.SampleRate > 0
                    ? (int)systemConfig.SampleRate
                    : 48000;

            var channels = outputConfig.Channels > 0
                ? (int)outputConfig.Channels
                : nativeFormat?.Channels > 0
                    ? (int)nativeFormat.Value.Channels
                : systemConfig.UseHrtf
                    ? 2
                    : systemConfig.Channels > 0
                        ? (int)systemConfig.Channels
                        : 2;

            return new SfAudioFormat
            {
                Format = SampleFormat.F32,
                Channels = channels,
                Layout = SfAudioFormat.GetLayoutFromChannels(channels),
                SampleRate = sampleRate
            };
        }

        private static NativeDataFormat? SelectNativeFormat(DeviceInfo? device, bool forceStereo, int preferredChannels)
        {
            if (device is not { SupportedDataFormats.Length: > 0 })
                return null;

            NativeDataFormat? firstValid = null;
            NativeDataFormat? channelMatch = null;
            NativeDataFormat? stereoMatch = null;

            for (var i = 0; i < device.Value.SupportedDataFormats.Length; i++)
            {
                var format = device.Value.SupportedDataFormats[i];
                if (format.SampleRate == 0 || format.Channels == 0)
                    continue;

                firstValid ??= format;
                if (format.Channels == 2)
                    stereoMatch ??= format;
                if (preferredChannels > 0 && format.Channels == preferredChannels)
                    channelMatch ??= format;
            }

            if (forceStereo)
                return stereoMatch ?? firstValid;

            return channelMatch ?? stereoMatch ?? firstValid;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private void UpdateLimiterMeters(float preLimiterPeak, float postLimiterPeak, float limiterGain)
        {
            _lastPreLimiterPeak = preLimiterPeak;
            _lastPostLimiterPeak = postLimiterPeak;
            _lastLimiterGain = limiterGain;
        }

        private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
        {
            var lengthSquared = value.LengthSquared();
            if (lengthSquared <= 1e-6f)
                return fallback;
            return Vector3.Normalize(value);
        }
    }
}
