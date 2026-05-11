using System;
using TopSpeed.Audio;
using TopSpeed.Input;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class VehicleRadioController : IDisposable
    {
        internal readonly struct PlaybackOptions
        {
            public PlaybackOptions(
                string busName,
                bool spatialize,
                bool allowHrtf,
                DriveSettings? settings,
                AudioVolumeCategory? volumeCategory,
                string tempFolderName)
            {
                BusName = string.IsNullOrWhiteSpace(busName) ? AudioEngineOptions.RadioBusName : busName;
                Spatialize = spatialize;
                AllowHrtf = allowHrtf;
                Settings = settings;
                VolumeCategory = volumeCategory;
                TempFolderName = string.IsNullOrWhiteSpace(tempFolderName) ? "Radio" : tempFolderName;
            }

            public string BusName { get; }
            public bool Spatialize { get; }
            public bool AllowHrtf { get; }
            public DriveSettings? Settings { get; }
            public AudioVolumeCategory? VolumeCategory { get; }
            public string TempFolderName { get; }
        }

        private readonly AudioManager _audio;
        private readonly PlaybackOptions _options;
        private Source? _source;
        private bool _desiredPlaying;
        private bool _pausedByGame;
        private string? _mediaPath;
        private string? _ownedTempFile;
        private uint _mediaId;
        private int _volumePercent = 100;
        private bool _loopPlayback = true;

        public VehicleRadioController(AudioManager audio)
            : this(
                audio,
                new PlaybackOptions(
                    AudioEngineOptions.RadioBusName,
                    spatialize: true,
                    allowHrtf: true,
                    settings: null,
                    volumeCategory: null,
                    tempFolderName: "Radio"))
        {
        }

        public VehicleRadioController(AudioManager audio, PlaybackOptions options)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _options = options;
        }

        public uint MediaId => _mediaId;
        public bool HasMedia => _source != null;
        public bool IsPlaying => _source != null && _source.IsPlaying;
        public bool IsPaused => _source != null && _source.IsPaused;
        public bool DesiredPlaying => _desiredPlaying;
        public string? MediaPath => _mediaPath;
        public int VolumePercent => _volumePercent;
        public bool LoopPlayback => _loopPlayback;

        public void SetVolumePercent(int volumePercent)
        {
            if (volumePercent < 0)
                volumePercent = 0;
            if (volumePercent > 100)
                volumePercent = 100;

            _volumePercent = volumePercent;
            ApplySourceVolume();
        }

        public void SetLoopPlayback(bool loopPlayback)
        {
            _loopPlayback = loopPlayback;
            if (_source == null)
                return;

            _source.SetLooping(_loopPlayback);
        }

        public void RefreshCategoryVolume()
        {
            ApplySourceVolume();
        }

        private void ApplySourceVolume()
        {
            if (_source == null)
                return;

            if (_options.Settings != null && _options.VolumeCategory.HasValue)
            {
                _source.SetVolumePercent(_options.Settings, _options.VolumeCategory.Value, _volumePercent);
                return;
            }

            _source.SetVolumePercent(_volumePercent);
        }
    }
}

