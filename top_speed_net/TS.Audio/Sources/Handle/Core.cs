using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using SoundFlow.Enums;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle : IDisposable
    {
        private static int _nextId;
        private readonly AudioOutput _output;
        private readonly AudioAsset _asset;
        private readonly bool _ownsAsset;
        private readonly AudioBus _bus;
        private readonly object _stateSync;
        private readonly SourcePlayer _player;
        private readonly VariableRateDataProvider _provider;
        private readonly bool _spatialize;
        private readonly bool _allowHrtf;
        private SteamAudioSpatialModifier? _steamAudioSpatial;
        private Action? _onEnd;
        private readonly List<Action> _endObservers;
        private float _userVolume = 1f;
        private float _currentVolume = 1f;
        private float _pitch = 1f;
        private float _pan;
        private float _spatialPan;
        private float _spatialPitch = 1f;
        private float _spatialGain = 1f;
        private float _distanceAttenuation = 1f;
        private float _fadeDuration;
        private float _fadeRemaining;
        private float _fadeStartVolume;
        private float _fadeTargetVolume;
        private bool _stopAfterFade;
        private bool _looping;
        private bool _stereoWidening;
        private Vector3 _position;
        private Vector3 _velocity;
        private DistanceModel _distanceModel;
        private float _minDistance;
        private float _maxDistance;
        private float _rollOff;
        private float? _curveDistanceScaler;
        private float _dopplerFactor = 1f;
        private RoomAcoustics _roomAcoustics;
        private bool _disposed;
        private AudioLifecycleState _lifecycleState;
        private AudioSourceSnapshot? _lastSnapshot;
        private int _controlApplyQueued;
        private PlaybackState _logicalPlaybackState;
        private bool _reachedEnd;
        private bool _pendingStartDiagnostic;

        internal AudioSourceHandle(AudioOutput output, AudioAsset asset, bool spatialize, bool useHrtf, AudioBus bus, bool ownsAsset = true)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _spatialize = spatialize;
            _allowHrtf = useHrtf;
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _ownsAsset = ownsAsset;
            SourceId = System.Threading.Interlocked.Increment(ref _nextId);
            _endObservers = new List<Action>();
            _stateSync = new object();
            _provider = new VariableRateDataProvider(_asset.CreateProvider(output.BackendEngine, output.SoundFlowFormat), output.SoundFlowFormat.Channels);
            _lifecycleState = AudioLifecycleState.Active;
            _player = new SourcePlayer(output.BackendEngine, output.SoundFlowFormat, _provider, HandleAudioThreadException)
            {
                Name = $"Source {SourceId}"
            };
            InitializeVolumeState();
            _player.PlaybackSpeed = 1f;
            _distanceModel = output.SystemConfig.DistanceModel;
            _minDistance = output.SystemConfig.MinDistance;
            _maxDistance = output.SystemConfig.MaxDistance;
            _rollOff = output.SystemConfig.RollOff;
            _curveDistanceScaler = output.SystemConfig.UseCurveDistanceScaler
                ? output.SystemConfig.CurveDistanceScaler
                : null;
            _dopplerFactor = output.SystemConfig.DopplerFactor;
            _steamAudioSpatial = CreateSpatialModifier();
            ApplyPan();
            ApplyPlaybackSpeed();
            _output.AttachSourceToGraph(_bus, _player, _steamAudioSpatial);

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceCreated,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source created.",
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["inputChannels"] = InputChannels,
                    ["inputSampleRate"] = InputSampleRate,
                    ["spatialize"] = spatialize,
                    ["useHrtf"] = useHrtf
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        public int SourceId { get; }
        public bool IsPlaying => !_disposed && (_logicalPlaybackState == PlaybackState.Playing || _player.State == PlaybackState.Playing);
        public bool IsPaused => !_disposed && (_logicalPlaybackState == PlaybackState.Paused || _player.State == PlaybackState.Paused);
        internal bool IsDisposed => _disposed;
        internal bool IsActive => !_disposed && _lifecycleState == AudioLifecycleState.Active;
        internal AudioLifecycleState LifecycleState => _lifecycleState;
        public int InputChannels => _asset.InputChannels;
        public int InputSampleRate => _asset.InputSampleRate;
        internal bool UsesSteamAudio => _steamAudioSpatial?.UsesSteamAudio == true;
        internal bool IsSpatialized => _spatialize;
        internal Vector3 WorldPosition => _position;
        internal Vector3 WorldVelocity => _velocity;
        internal DistanceModel DistanceModel => _distanceModel;
        internal RoomAcoustics RoomAcoustics => _roomAcoustics;
        internal bool StereoWideningEnabled => _stereoWidening;
        internal float ReferenceDistance => GetEffectiveMinDistance(_minDistance, _curveDistanceScaler);
        internal float MaximumDistance => GetEffectiveMaxDistance(ReferenceDistance, _maxDistance, _curveDistanceScaler);
        internal float Rolloff => _rollOff;

        internal AudioSourceSnapshot CaptureSnapshot()
        {
            if (_lifecycleState == AudioLifecycleState.Disposed && _lastSnapshot != null)
                return _lastSnapshot;

            try
            {
                return CaptureSnapshotCore();
            }
            catch (ObjectDisposedException)
            {
                return _lastSnapshot ?? CaptureFallbackSnapshot();
            }
            catch (InvalidOperationException)
            {
                return _lastSnapshot ?? CaptureFallbackSnapshot();
            }
        }

        private AudioSourceSnapshot CaptureSnapshotCore()
        {
            var busVolume = _bus.GetEffectiveVolume();
            var effectiveVolume = _currentVolume * _spatialGain;
            var estimatedMix = effectiveVolume * busVolume * _output.GetMasterVolume();
            _provider.CaptureCursorState(out var providerPositionSamples, out var innerPositionSamples, out var bufferedFrames);
            var snapshot = new AudioSourceSnapshot(
                SourceId,
                _bus.Name,
                IsPlaying,
                _spatialize,
                UsesSteamAudio,
                InputChannels,
                InputSampleRate,
                _looping,
                _currentVolume,
                AudioMath.GainToDecibels(_currentVolume),
                _spatialGain,
                _distanceAttenuation,
                _pitch,
                _pan,
                busVolume,
                AudioMath.GainToDecibels(busVolume),
                estimatedMix,
                AudioMath.GainToDecibels(estimatedMix),
                _bus.CaptureGainStages(),
                GetLengthSeconds(),
                _asset.DebugName,
                _output.SampleRate,
                _provider.SampleRate,
                providerPositionSamples,
                innerPositionSamples,
                bufferedFrames,
                _player.Time);
            _lastSnapshot = snapshot;
            return snapshot;
        }

        private AudioSourceSnapshot CaptureFallbackSnapshot()
        {
            var busVolume = 1f;
            try
            {
                busVolume = _bus.GetEffectiveVolume();
            }
            catch
            {
            }

            var effectiveVolume = _currentVolume * _spatialGain;
            var estimatedMix = effectiveVolume * busVolume * _output.GetMasterVolume();
            var snapshot = new AudioSourceSnapshot(
                SourceId,
                _bus.Name,
                false,
                _spatialize,
                false,
                InputChannels,
                InputSampleRate,
                _looping,
                _currentVolume,
                AudioMath.GainToDecibels(_currentVolume),
                _spatialGain,
                _distanceAttenuation,
                _pitch,
                _pan,
                busVolume,
                AudioMath.GainToDecibels(busVolume),
                estimatedMix,
                AudioMath.GainToDecibels(estimatedMix),
                Array.Empty<AudioGainStageSnapshot>(),
                GetLengthSecondsSafe(),
                _asset.DebugName,
                _output.SampleRate);
            _lastSnapshot = snapshot;
            return snapshot;
        }

        public void Dispose()
        {
            lock (_stateSync)
            {
                if (_disposed)
                    return;

                SetLifecycleStateUnsafe(AudioLifecycleState.Stopping, "Audio source disposal requested.");
                CancelFadeUnsafe();
                _pendingStartDiagnostic = false;
                _player.Silence();
            }

            _output.EnqueueLifecycle(DisposeFromGraph, "source-dispose-from-graph");
        }

        private void DispatchPlaybackEndedIfNeeded()
        {
            if (_disposed || !_player.ConsumePlaybackEnded())
                return;

            OnPlaybackEnded();
        }

        private void OnPlaybackEnded()
        {
            if (_disposed)
                return;

            _pendingStartDiagnostic = false;
            _reachedEnd = true;
            _logicalPlaybackState = PlaybackState.Stopped;
            Action? onEnd;
            Action[]? observers = null;
            lock (_stateSync)
            {
                onEnd = _onEnd;
                if (_endObservers.Count > 0)
                    observers = _endObservers.ToArray();
            }

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceEnded,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source reached the end of playback.",
                new Dictionary<string, object?> { ["sourceId"] = SourceId },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            onEnd?.Invoke();

            if (observers == null)
                return;

            for (var i = 0; i < observers.Length; i++)
                observers[i]();
        }

        private void EmitStartedDiagnosticIfNeeded()
        {
            if (_disposed || !_pendingStartDiagnostic)
                return;

            _provider.CaptureCursorState(out var providerPositionSamples, out _, out _);
            if (providerPositionSamples <= 0)
                return;

            lock (_stateSync)
            {
                if (!_pendingStartDiagnostic)
                    return;

                _pendingStartDiagnostic = false;
            }

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceStarted,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                SourceId,
                "Audio source playback started advancing.",
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["providerPositionSamples"] = providerPositionSamples
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        private bool ShouldEmitSourceDiagnostic(AudioDiagnosticKind kind, AudioDiagnosticLevel level = AudioDiagnosticLevel.Debug)
        {
            return _output.Diagnostics.ShouldEmit(level, kind, AudioDiagnosticEntityType.Source, _output.Name, _bus.Name, SourceId);
        }

        private void QueueApplyControlState()
        {
            if (!IsActive)
                return;

            if (Interlocked.Exchange(ref _controlApplyQueued, 1) != 0)
                return;

            if (!_output.EnqueueControl(ApplyQueuedControlState, "source-apply-control-state"))
                Interlocked.Exchange(ref _controlApplyQueued, 0);
        }

        private void ApplyQueuedControlState()
        {
            Interlocked.Exchange(ref _controlApplyQueued, 0);
            if (!IsActive)
                return;

            lock (_stateSync)
            {
                if (!IsActive)
                    return;

                ApplyPan();
                ApplyPlaybackSpeed();
            }
        }

        private void InitializeVolumeState()
        {
            _userVolume = 1f;
            _currentVolume = _userVolume;
            _fadeDuration = 0f;
            _fadeRemaining = 0f;
            _fadeStartVolume = _currentVolume;
            _fadeTargetVolume = _currentVolume;
            _stopAfterFade = false;
        }

        private SteamAudioSpatialModifier? CreateSpatialModifier()
        {
            if (!_spatialize)
                return null;

            var runtime = _output.SteamAudioRuntime;
            if (runtime == null || !runtime.IsAvailable)
                return null;

            return new SteamAudioSpatialModifier(runtime, _output.Channels, _allowHrtf && _output.IsHrtfActive, _output.SystemConfig.HrtfDownmixMode, HandleAudioThreadException);
        }

        internal void QueueRefreshSteamAudioSpatial()
        {
            _output.EnqueueLifecycle(RefreshSteamAudioSpatialCore, "source-refresh-steam-audio");
        }

        private void RefreshSteamAudioSpatialCore()
        {
            if (!IsActive || !_spatialize)
                return;

            var replacement = CreateSpatialModifier();
            if (ReferenceEquals(replacement, _steamAudioSpatial))
                return;

            var oldSpatializer = _steamAudioSpatial;

            _steamAudioSpatial = replacement;
            _output.ReplaceSourceSpatializer(_player, oldSpatializer, _steamAudioSpatial);
            if (oldSpatializer != null)
                _output.EnqueueDeferredLifecycle(oldSpatializer.Dispose, "source-dispose-replaced-spatializer");

            lock (_stateSync)
                ApplyPan();
        }

        private void DisposeFromGraph()
        {
            var snapshot = CaptureSnapshot();
            SetLifecycleState(AudioLifecycleState.QueuedForDispose, "Audio source queued for graph detach.");
            var spatializer = _steamAudioSpatial;
            _steamAudioSpatial = null;

            _output.DetachSourceFromGraph(_bus, _player, spatializer);
            _output.RemoveSource(this);
            SetLifecycleState(AudioLifecycleState.Disposing, "Audio source detached from graph.");
            _output.EnqueueDeferredLifecycle(() => DisposeNativeCore(snapshot, spatializer), "source-dispose-native");
        }

        private void DisposeNativeCore(AudioSourceSnapshot snapshot, SteamAudioSpatialModifier? spatializer)
        {
            spatializer?.Dispose();
            _player.Dispose();
            _provider.Dispose();
            if (_ownsAsset)
                _asset.Dispose();
            _lastSnapshot = snapshot;
            SetLifecycleState(AudioLifecycleState.Disposed, "Audio source native resources disposed.");
            _output.Diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceDisposed,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                null,
                "Audio source disposed.",
                null,
                new AudioDiagnosticSnapshot(source: snapshot));
        }

        private void SetLifecycleState(AudioLifecycleState state, string message)
        {
            lock (_stateSync)
                SetLifecycleStateUnsafe(state, message);
        }

        private void SetLifecycleStateUnsafe(AudioLifecycleState state, string message)
        {
            if (_lifecycleState == state)
                return;

            var previous = _lifecycleState;
            _lifecycleState = state;
            _disposed = state != AudioLifecycleState.Active;
            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.SourceLifecycleChanged,
                AudioDiagnosticEntityType.Source,
                _output.Name,
                _bus.Name,
                SourceId,
                message,
                new Dictionary<string, object?>
                {
                    ["sourceId"] = SourceId,
                    ["previousState"] = previous.ToString(),
                    ["state"] = state.ToString()
                },
                () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
        }

        private void HandleAudioThreadException(Exception exception)
        {
            if (exception == null)
                return;

            var exceptionType = exception.GetType().FullName;
            var message = exception.Message;
            lock (_stateSync)
            {
                if (_lifecycleState != AudioLifecycleState.Active)
                    return;

                CancelFadeUnsafe();
                _pendingStartDiagnostic = false;
                _player.Silence();
            }

            _output.EnqueueLifecycle(
                () => _output.Diagnostics.Emit(
                    AudioDiagnosticLevel.Error,
                    AudioDiagnosticKind.SourceAudioThreadException,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source processing failed on the audio thread and was silenced.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["exceptionType"] = exceptionType,
                        ["message"] = message
                    },
                    new AudioDiagnosticSnapshot(source: CaptureSnapshot())),
                "source-audio-thread-exception");
        }

        private float GetLengthSecondsSafe()
        {
            try
            {
                return GetLengthSeconds();
            }
            catch
            {
                return 0f;
            }
        }

        internal void ApplyDirectSimulation(float occlusion, float airLow, float airMid, float airHigh, float transmissionLow, float transmissionMid, float transmissionHigh)
        {
            if (!IsActive)
                return;

            _steamAudioSpatial?.ApplyDirectSimulation(occlusion, airLow, airMid, airHigh, transmissionLow, transmissionMid, transmissionHigh);
        }

        internal void ClearDirectSimulation()
        {
            if (!IsActive)
                return;

            _steamAudioSpatial?.ClearDirectSimulation();
        }

        internal void ApplyReverbSimulation(float timeLow, float timeMid, float timeHigh, float eqLow, float eqMid, float eqHigh, int delay, float wetScale)
        {
            if (!IsActive)
                return;

            _steamAudioSpatial?.ApplyReverbSimulation(timeLow, timeMid, timeHigh, eqLow, eqMid, eqHigh, delay, wetScale);
        }

        internal void ClearReverbSimulation()
        {
            if (!IsActive)
                return;

            _steamAudioSpatial?.ClearReverbSimulation();
        }

        private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback)
        {
            var lengthSquared = value.LengthSquared();
            if (lengthSquared <= 1e-6f)
                return fallback;
            return Vector3.Normalize(value);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }
    }
}
