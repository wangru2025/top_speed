using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Core.Settings;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Runtime;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Drive.Panels
{
    internal sealed partial class RadioVehiclePanel : IVehicleRacePanel
    {
        private const int VolumeStepPercent = 10;

        private readonly DriveInput _input;
        private readonly AudioManager _audio;
        private readonly DriveSettings _settings;
        private readonly VehicleRadioController _radio;
        private readonly IFileDialogs _fileDialogs;
        private readonly Func<uint> _nextMediaId;
        private readonly Action<string> _announce;
        private readonly Action<uint, string>? _mediaLoaded;
        private readonly Action<bool, bool, uint>? _playbackChanged;
        private readonly object _pendingPathLock = new object();
        private readonly List<string> _playlist = new List<string>();
        private readonly Random _random = new Random();

        private volatile bool _pickerInProgress;
        private volatile bool _folderPickerInProgress;
        private string? _pendingSelectedPath;
        private string? _pendingSelectedFolder;
        private string _playlistFolder = string.Empty;
        private int _playlistIndex = -1;
        private bool _shuffleMode;
        private bool _loopMode;
        private bool _lastObservedPlaying;
        private SoundAsset? _volumeUpSound;
        private SoundAsset? _volumeDownSound;

        public RadioVehiclePanel(
            DriveInput input,
            AudioManager audio,
            DriveSettings settings,
            VehicleRadioController radio,
            IFileDialogs fileDialogs,
            Func<uint> nextMediaId,
            Action<string> announce,
            Action<uint, string>? mediaLoaded = null,
            Action<bool, bool, uint>? playbackChanged = null)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _nextMediaId = nextMediaId ?? throw new ArgumentNullException(nameof(nextMediaId));
            _announce = announce ?? throw new ArgumentNullException(nameof(announce));
            _mediaLoaded = mediaLoaded;
            _playbackChanged = playbackChanged;
            _shuffleMode = _settings.RadioShuffle;
            _loopMode = false;
            TryRestoreFolderPlaylist();
            ApplyLoopMode();
        }

        public string Name => LocalizationService.Mark("Radio");
        public bool AllowsDrivingInput => false;
        public bool AllowsAuxiliaryInput => false;

        public void Tick(float elapsed)
        {
            ProcessPendingSelection();
            ProcessPendingFolderSelection();
            HandlePlaybackEndAdvance();
        }

        public void Update(float elapsed)
        {
            Tick(elapsed);

            if (_input.GetOpenRadioMediaRequest())
                OpenRadioMedia();

            if (_input.GetOpenRadioFolderRequest())
                OpenRadioFolder();

            if (_input.GetToggleRadioPlaybackRequest())
                TogglePlayback();

            if (_input.GetRadioNextTrackRequest())
                CycleTrack(1);
            else if (_input.GetRadioPreviousTrackRequest())
                CycleTrack(-1);

            if (_input.GetRadioToggleShuffleRequest())
                ToggleShuffle();

            if (_input.GetRadioToggleLoopRequest())
                ToggleLoop();

            if (_input.GetRadioVolumeUpRequest())
                AdjustVolume(VolumeStepPercent, "volume_up.ogg");
            else if (_input.GetRadioVolumeDownRequest())
                AdjustVolume(-VolumeStepPercent, "volume_down.ogg");
        }

        public void Pause()
        {
            _radio.PauseForGame();
        }

        public void Resume()
        {
            _radio.ResumeFromGame();
        }

        public void Dispose()
        {
            _volumeUpSound = null;
            _volumeDownSound = null;
        }
    }
}




