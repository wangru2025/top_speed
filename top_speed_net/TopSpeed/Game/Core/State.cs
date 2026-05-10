using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Core.Settings;
using TopSpeed.Core.Updates;
using TopSpeed.Data;
using TopSpeed.Drive.Single;
using TopSpeed.Drive.TimeTrial;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Drive;
using TopSpeed.Runtime;
using TopSpeed.Shortcuts;
using TopSpeed.Speech;
using CoreRaceMode = TopSpeed.Core.DriveMode;
using TS.Audio;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private enum AppState
        {
            Logo,
            Menu,
            TimeTrial,
            SingleRace,
            MultiplayerRace,
            Paused,
            Calibration
        }

        private readonly IWindowHost _window;
        private readonly ITextInputService _textInput;
        private readonly IFileDialogs _fileDialogs;
        private readonly IClipboardService _clipboard;
        private readonly IGameAudio _audio;
        private readonly IGameSpeech _speech;
        private readonly IInputService _input;
        private readonly MenuManager _menu;
        private readonly DialogManager _dialogs;
        private readonly ChoiceDialogManager _choices;
        private readonly ResultShow _resultShow;
        private readonly DriveSettings _settings;
        private readonly IReadOnlyList<SettingsIssue> _settingsIssues;
        private readonly bool _settingsFileMissing;
        private readonly IReadOnlyList<ClientLanguage> _clientLanguages;
        private readonly DriveInput _driveInput;
        private readonly DriveSetup _setup;
        private readonly IDriveSessionFactory _driveSessionFactory;
        private readonly StateMachine _stateMachine;
        private readonly SettingsManager _settingsManager;
        private readonly DriveSelection _selection;
        private readonly MenuRegistry _menuRegistry;
        private readonly IMultiplayerRuntime _multiplayerCoordinator;
        private readonly IMultiplayerMenuTouch _multiplayerMenuTouch;
        private readonly MultiplayerConnector _multiplayerConnector;
        private readonly SessionReconnector _sessionReconnector;
        private readonly UpdateConfig _updateConfig;
        private readonly UpdateService _updateService;
        private readonly MultiplayerDispatch _multiplayerDispatch;
        private MultiplayerSession? _session;
        private readonly InputMappingHandler _inputMapping;
        private readonly ShortcutMappingHandler _shortcutMapping;
        private LogoScreen? _logo;
        private AppState _state;
        private AppState _pausedState;
        private bool _needsCalibration;
        private bool _autoUpdateAfterCalibration;
        private bool _calibrationMenusRegistered;
        private string? _calibrationReturnMenuId;
        private bool _calibrationOverlay;
        private Stopwatch? _calibrationStopwatch;
        private bool _pendingDriveStart;
        private CoreRaceMode _pendingMode;
        private int _lastSingleRacePlayerNumber = -1;
        private readonly Queue<int> _singleRacePlayerNumberBag = new Queue<int>();
        private int _singleRacePlayerNumberSlots = -1;
        private bool _pauseKeyReleased = true;
        private TimeTrialSession? _timeTrial;
        private SingleSession? _singleRace;
        private readonly MultiplayerRaceRuntime _multiplayerRaceRuntime;
        private readonly Multiplayer.Communicator.MultiplayerCommunicatorRuntime _multiplayerCommunicatorRuntime;
        private bool _updateCheckQueued;
        private bool _updatePromptShown;
        private Task<UpdateCheckResult>? _updateCheckTask;
        private UpdateInfo? _pendingUpdateInfo;
        private Task<DownloadResult>? _updateDownloadTask;
        private CancellationTokenSource? _updateDownloadCts;
        private long _updateTotalBytes;
        private long _updateDownloadedBytes;
        private int _updatePercent;
        private int _updateTonePercent;
        private int _lastSpokenUpdatePercent;
        private bool _updateDownloadCanceledByUser;
        private bool _updateProgressOpen;
        private bool _updateCompleteOpen;
        private string _updateZipPath = string.Empty;
        private bool _manualUpdateRequest;
        private Task<LatestChangesResult>? _latestChangesTask;
        private bool _textInputPromptActive;
        private Action<TextInputResult>? _textInputPromptCallback;
        private SoundAsset? _raceWinSound;
        public bool IsModalInputActive { get; private set; }
        internal int LoopIntervalMs => IsMenuState(_state) ? 15 : 8;

        private const string CalibrationIntroMenuId = "calibration_intro";
        private const string CalibrationSampleMenuId = "calibration_sample";
        private static readonly string CalibrationInstructions = InteractionHints.ForPlatform(
            LocalizationService.Mark("Screen-reader calibration. You'll be presented with a short piece of text on the next screen. Press ENTER when your screen-reader finishes speaking it."),
            LocalizationService.Mark("Screen-reader calibration. You'll be presented with a short piece of text on the next screen. Swipe up when your screen-reader finishes speaking it."));
        private static readonly string CalibrationSampleText = LocalizationService.Mark(
            "I really have nothing interesting to put here not even the secret to life except this really long run on sentence that is probably the most boring thing you have ever read but that will help me get an idea of how fast your screen reader is speaking.");

        public event Action? ExitRequested;
    }
}





