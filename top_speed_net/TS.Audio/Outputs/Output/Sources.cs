using System;
using System.Collections.Generic;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        internal AudioSourceHandle CreateSource(AudioAsset asset, bool spatialize, bool useHrtf, AudioBus? bus, bool ownsAsset)
        {
            ThrowIfDisposed();
            var source = new AudioSourceHandle(this, asset, spatialize, useHrtf, bus ?? _mainBus, ownsAsset);
            source.SetDopplerFactor(_systemConfig.DopplerFactor);
            source.SetRoomAcoustics(_roomAcoustics);

            lock (_sourceLock)
                _sources.Add(source);
            return source;
        }

        public AudioSourceHandle CreateSource(string filePath, bool streamFromDisk = true, bool useHrtf = true)
        {
            return CreateSource(new FileAsset(filePath, streamFromDisk), spatialize: useHrtf, useHrtf: useHrtf, bus: null, ownsAsset: true);
        }

        public Source CreateSource(SoundAsset asset, bool spatialize = false, bool useHrtf = false)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            return new Source(CreateSource(asset.Asset, spatialize, useHrtf, bus: null, ownsAsset: false), ownsHandle: true);
        }

        public AudioSourceHandle CreateSpatialSource(string filePath, bool streamFromDisk = true, bool allowHrtf = true)
        {
            return CreateSource(new FileAsset(filePath, streamFromDisk), spatialize: true, useHrtf: allowHrtf, bus: null, ownsAsset: true);
        }

        public Source CreateSpatialSource(SoundAsset asset, bool allowHrtf = true)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            return new Source(CreateSource(asset.Asset, spatialize: true, useHrtf: allowHrtf, bus: null, ownsAsset: false), ownsHandle: true);
        }

        public AudioSourceHandle CreateProceduralSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, bool useHrtf = true)
        {
            return CreateSource(new ProceduralAsset(callback, channels, sampleRate), spatialize: useHrtf, useHrtf: useHrtf, bus: null, ownsAsset: true);
        }

        public Source CreateProceduralOwnedSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, bool spatialize = false, bool useHrtf = false)
        {
            return new Source(CreateSource(new ProceduralAsset(callback, channels, sampleRate), spatialize, useHrtf, bus: null, ownsAsset: true), ownsHandle: true);
        }

        public TrackStream CreateStream(params string[] filePaths)
        {
            return CreateStream(_mainBus, filePaths);
        }

        public TrackStream CreateStream(params StreamAsset[] assets)
        {
            return CreateStream(_mainBus, assets);
        }

        internal TrackStream CreateStream(AudioBus bus, params string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
                throw new ArgumentException("At least one file path is required.", nameof(filePaths));

            var assets = new StreamAsset[filePaths.Length];
            for (var i = 0; i < filePaths.Length; i++)
                assets[i] = new StreamAsset(filePaths[i]);
            return CreateStream(bus, ownsAssets: true, assets);
        }

        internal TrackStream CreateStream(AudioBus bus, params StreamAsset[] assets)
        {
            return CreateStream(bus, ownsAssets: false, assets);
        }

        private TrackStream CreateStream(AudioBus bus, bool ownsAssets, params StreamAsset[] assets)
        {
            ThrowIfDisposed();
            var stream = new TrackStream(this, bus, ownsAssets, assets);
            lock (_sourceLock)
                _streams.Add(stream);
            _diagnostics.Emit(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.StreamCreated,
                AudioDiagnosticEntityType.Stream,
                Name,
                bus.Name,
                null,
                "Audio stream created.",
                new Dictionary<string, object?>
                {
                    ["trackCount"] = assets.Length
                });
            return stream;
        }

        public void Update(double deltaTime)
        {
            DrainLifecycle();
            DrainControl();
            SyncSteamAudioRuntime();
            DrainLifecycle();
            DrainControl();

            _sourceUpdateSnapshot.Clear();
            _streamUpdateSnapshot.Clear();
            lock (_sourceLock)
            {
                for (var i = 0; i < _sources.Count; i++)
                {
                    if (_sources[i].IsActive)
                        _sourceUpdateSnapshot.Add(_sources[i]);
                }

                _streamUpdateSnapshot.AddRange(_streams);
            }

            for (var i = 0; i < _streamUpdateSnapshot.Count; i++)
                _streamUpdateSnapshot[i].Update();

            for (var i = 0; i < _sourceUpdateSnapshot.Count; i++)
                _sourceUpdateSnapshot[i].Update(deltaTime);

            _steamAudioRuntime?.UpdateSimulation(_sourceUpdateSnapshot);
            _sourceUpdateSnapshot.Clear();
            _streamUpdateSnapshot.Clear();
            DrainControl();
            DrainLifecycle();
        }
    }
}
