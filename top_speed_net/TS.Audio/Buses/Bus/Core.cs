using System;
using System.Collections.Generic;
using SoundFlow.Abstracts;
using SfMixer = SoundFlow.Components.Mixer;

namespace TS.Audio
{
    public sealed partial class AudioBus : IDisposable
    {
        private readonly AudioOutput _output;
        private readonly AudioBus? _parent;
        private readonly PlaybackPolicy _defaults;
        private readonly List<AudioBus> _children;
        private readonly List<BusEffect> _effects;
        private readonly List<SoundModifier> _attachedModifiers;
        private readonly object _effectLock;
        private readonly SfMixer _mixer;
        private int _effectVersion;
        private float _localVolume = 1f;
        private float _effectiveVolume = 1f;
        private bool _muted;
        private bool _effectsEnabled = true;
        private bool _disposed;

        internal AudioBus(AudioOutput output, string name, AudioBus? parent, PlaybackPolicy? defaults = null)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            Name = string.IsNullOrWhiteSpace(name) ? "main" : name;
            _parent = parent;
            _defaults = defaults?.Clone() ?? new PlaybackPolicy();
            _children = new List<AudioBus>();
            _effects = new List<BusEffect>();
            _attachedModifiers = new List<SoundModifier>();
            _effectLock = new object();
            _mixer = new SfMixer(output.BackendEngine, output.SoundFlowFormat)
            {
                Name = $"{Name} Bus"
            };

            _output.AttachBusToGraph(_parent, _mixer);

            _parent?._children.Add(this);
            RecalculateMix();
            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Info,
                AudioDiagnosticKind.BusCreated,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                "Audio bus created.",
                new Dictionary<string, object?>
                {
                    ["parentName"] = _parent?.Name,
                    ["defaultsSpatialize"] = _defaults.Spatialize,
                    ["defaultsUseHrtf"] = _defaults.UseHrtf
                },
                () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
        }

        public string Name { get; }
        public AudioBus? Parent => _parent;
        public IReadOnlyList<AudioBus> Children => _children;
        public bool Muted => _muted;
        public bool EffectsEnabled => _effectsEnabled;
        public PlaybackPolicy Defaults => _defaults;
        internal SfMixer Mixer => _mixer;

        public Source CreateSource(SoundAsset asset, bool? spatialize = null, bool? useHrtf = null)
        {
            ThrowIfDisposed();
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var resolved = ResolveOptions(new SourceOptions
            {
                Spatialize = spatialize,
                UseHrtf = useHrtf
            });
            var source = CreateResolvedSource(asset.Asset, ownsAsset: false, resolved);
            ConfigureSource(source, resolved);
            return source;
        }

        public Source CreateSource(string filePath, bool streamFromDisk = true, bool? spatialize = null, bool? useHrtf = null)
        {
            ThrowIfDisposed();
            var resolved = ResolveOptions(new SourceOptions
            {
                Spatialize = spatialize,
                UseHrtf = useHrtf
            });
            var source = CreateResolvedSource(new FileAsset(filePath, streamFromDisk), ownsAsset: true, resolved);
            ConfigureSource(source, resolved);
            return source;
        }

        public Source CreateSpatialSource(SoundAsset asset, bool? allowHrtf = null)
        {
            return CreateSource(asset, spatialize: true, useHrtf: allowHrtf);
        }

        public Source CreateSpatialSource(string filePath, bool streamFromDisk = true, bool? allowHrtf = null)
        {
            return CreateSource(filePath, streamFromDisk, spatialize: true, useHrtf: allowHrtf);
        }

        public Source CreateProceduralSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, bool? spatialize = null, bool? useHrtf = null)
        {
            ThrowIfDisposed();
            var resolved = ResolveOptions(new SourceOptions
            {
                Spatialize = spatialize,
                UseHrtf = useHrtf
            });
            var source = CreateResolvedSource(new ProceduralAsset(callback, channels, sampleRate), ownsAsset: true, resolved);
            ConfigureSource(source, resolved);
            return source;
        }

        public Source CreateSource(SoundAsset asset, SourceOptions options)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var resolved = ResolveOptions(options);
            var source = CreateResolvedSource(asset.Asset, ownsAsset: false, resolved);
            ConfigureSource(source, resolved);
            return source;
        }

        public Source Play(SoundAsset asset, SourceOptions options)
        {
            var resolved = ResolveOptions(options);
            var source = CreateResolvedSource(asset.Asset, ownsAsset: false, resolved);
            ConfigureSource(source, resolved);
            source.Play(resolved.Loop, resolved.FadeInSeconds);
            return source;
        }

        public Source CreateSource(string filePath, SourceOptions options, bool streamFromDisk = true)
        {
            var resolved = ResolveOptions(options);
            var source = CreateResolvedSource(new FileAsset(filePath, streamFromDisk), ownsAsset: true, resolved);
            ConfigureSource(source, resolved);
            return source;
        }

        public TrackStream CreateStream(params StreamAsset[] assets)
        {
            ThrowIfDisposed();
            return _output.CreateStream(this, assets);
        }

        public TrackStream CreateStream(params string[] filePaths)
        {
            ThrowIfDisposed();
            return _output.CreateStream(this, filePaths);
        }

        public Source Play(SoundAsset asset, bool? loop = null, bool? spatialize = null, bool? useHrtf = null)
        {
            var resolved = ResolveOptions(new SourceOptions
            {
                Loop = loop,
                Spatialize = spatialize,
                UseHrtf = useHrtf
            });
            var source = CreateResolvedSource(asset.Asset, ownsAsset: false, resolved);
            ConfigureSource(source, resolved);
            source.Play(resolved.Loop);
            return source;
        }

        public Source Play(string filePath, bool streamFromDisk = true, bool? loop = null, bool? spatialize = null, bool? useHrtf = null)
        {
            var resolved = ResolveOptions(new SourceOptions
            {
                Loop = loop,
                Spatialize = spatialize,
                UseHrtf = useHrtf
            });
            var source = CreateResolvedSource(new FileAsset(filePath, streamFromDisk), ownsAsset: true, resolved);
            ConfigureSource(source, resolved);
            source.Play(resolved.Loop);
            return source;
        }

        public AudioBus CreateChild(string name)
        {
            return _output.CreateBus(name, this, _defaults.Clone());
        }

        internal void ApplyDefaults(PlaybackPolicy defaults)
        {
            CopyPolicy(defaults, _defaults);
        }

        internal void RemoveEffect(BusEffect effect)
        {
            if (effect == null)
                return;

            lock (_effectLock)
            {
                if (!_effects.Remove(effect))
                    return;

                QueueRebuildModifierChainUnsafe();
            }

            effect.MarkDetached();
        }

        internal void UpdateEffectState(BusEffect effect)
        {
            if (effect == null)
                return;

            lock (_effectLock)
            {
                if (!_effects.Contains(effect))
                    return;

                QueueEffectStateApplyUnsafe();
            }
        }

        public AudioBusSnapshot CaptureSnapshot()
        {
            lock (_effectLock)
            {
                var names = new List<string>(_effects.Count);
                for (var i = 0; i < _effects.Count; i++)
                    names.Add(_effects[i].Name);

                return new AudioBusSnapshot(
                    Name,
                    _parent?.Name,
                    _localVolume,
                    AudioMath.GainToDecibels(_localVolume),
                    _effectiveVolume,
                    AudioMath.GainToDecibels(_effectiveVolume),
                    _muted,
                    _children.Count,
                    _effectsEnabled,
                    _effects.Count,
                    names,
                    CaptureGainStages());
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            var snapshot = CaptureSnapshot();
            var childSnapshot = _children.ToArray();
            for (var i = 0; i < childSnapshot.Length; i++)
                childSnapshot[i].Dispose();

            ClearEffects();
            _parent?._children.Remove(this);
            _output.UnregisterBus(this);
            _output.EnqueueLifecycle(() => DisposeGraph(snapshot), "bus-dispose-from-graph");
        }

        private void DisposeGraph(AudioBusSnapshot snapshot)
        {
            _output.DetachBusFromGraph(_parent, _mixer);
            _output.EnqueueDeferredLifecycle(
                () =>
                {
                    _mixer.Dispose();
                    _output.Diagnostics.Emit(
                        AudioDiagnosticLevel.Info,
                        AudioDiagnosticKind.BusDisposed,
                        AudioDiagnosticEntityType.Bus,
                        _output.Name,
                        Name,
                        null,
                        "Audio bus disposed.",
                        null,
                        new AudioDiagnosticSnapshot(bus: snapshot));
                },
                "bus-dispose-native");
        }

        private ResolvedSourceOptions ResolveOptions(PlaybackPolicy? overrides)
        {
            return ResolvedSourceOptions.Merge(null, _defaults, overrides);
        }

        private Source CreateResolvedSource(AudioAsset asset, bool ownsAsset, ResolvedSourceOptions options)
        {
            return new Source(_output.CreateSource(asset, options.Spatialize, options.UseHrtf, this, ownsAsset), ownsHandle: true);
        }

        internal IReadOnlyList<AudioGainStageSnapshot> CaptureGainStages()
        {
            var chain = new Stack<AudioBus>();
            for (var cursor = this; cursor != null; cursor = cursor._parent)
                chain.Push(cursor);

            var stages = new List<AudioGainStageSnapshot>(chain.Count);
            while (chain.Count > 0)
            {
                var bus = chain.Pop();
                var linear = bus._muted ? 0f : Clamp01(bus._localVolume);
                stages.Add(new AudioGainStageSnapshot(bus.Name, linear, AudioMath.GainToDecibels(linear)));
            }

            return stages;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AudioBus));
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static void CopyPolicy(PlaybackPolicy source, PlaybackPolicy target)
        {
            target.Spatialize = source.Spatialize;
            target.UseHrtf = source.UseHrtf;
            target.Loop = source.Loop;
            target.FadeInSeconds = source.FadeInSeconds;
            target.Volume = source.Volume;
            target.Pitch = source.Pitch;
            target.Pan = source.Pan;
            target.StereoWidening = source.StereoWidening;
            target.Position = source.Position;
            target.Velocity = source.Velocity;
            target.CurveDistanceScaler = source.CurveDistanceScaler;
            target.DopplerFactor = source.DopplerFactor;
            target.RoomAcoustics = source.RoomAcoustics;
            target.DistanceModel = source.DistanceModel;
            target.RefDistance = source.RefDistance;
            target.MaxDistance = source.MaxDistance;
            target.RollOff = source.RollOff;
        }
    }
}
