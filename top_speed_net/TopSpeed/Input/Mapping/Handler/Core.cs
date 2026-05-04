using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Localization;
using TopSpeed.Speech;
using TopSpeed.Input.Devices.Controller;

namespace TopSpeed.Input
{
    internal sealed partial class InputMappingHandler
    {
        private readonly IInputService _input;
        private readonly DriveInput _driveInput;
        private readonly DriveSettings _settings;
        private readonly SpeechService _speech;
        private readonly Action _saveSettings;
        private readonly Action<string, string?, IReadOnlyDictionary<int, string>, bool, string?, Action<bool, int>> _showChoiceDialog;

        private bool _mappingActive;
        private InputMappingMode _mappingMode;
        private DriveIntent _mappingAction;
        private bool _mappingNeedsInstruction;
        private State _mappingPrevController;
        private bool _mappingHasPrevController;

        public InputMappingHandler(
            IInputService input,
            DriveInput driveInput,
            DriveSettings settings,
            SpeechService speech,
            Action saveSettings,
            Action<string, string?, IReadOnlyDictionary<int, string>, bool, string?, Action<bool, int>> showChoiceDialog)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _driveInput = driveInput ?? throw new ArgumentNullException(nameof(driveInput));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
            _showChoiceDialog = showChoiceDialog ?? throw new ArgumentNullException(nameof(showChoiceDialog));
        }

        public bool IsActive => _mappingActive;

        public void BeginMapping(InputMappingMode mode, DriveIntent action)
        {
            if (mode == InputMappingMode.Controller)
            {
                if (_input.VibrationDevice == null || !_input.VibrationDevice.IsAvailable)
                {
                    _speech.Speak(LocalizationService.Mark("No controller detected."));
                    return;
                }
            }

            _mappingActive = true;
            _mappingMode = mode;
            _mappingAction = action;
            _mappingHasPrevController = false;
            _mappingNeedsInstruction = true;
        }

        public void Update()
        {
            if (!_mappingActive)
                return;

            if (_mappingNeedsInstruction)
            {
                _mappingNeedsInstruction = false;
                var instruction = _driveInput.KeyMap.GetMappingInstruction(_mappingMode == InputMappingMode.Keyboard, _mappingAction);
                _speech.Speak(instruction);
            }

            if (_input.WasPressed(Key.Escape))
            {
                _mappingActive = false;
                _speech.Speak(LocalizationService.Mark("Mapping cancelled."));
                return;
            }

            if (_mappingMode == InputMappingMode.Keyboard)
                TryCaptureKeyboardMapping();
            else
                TryCaptureControllerMapping();
        }

        public string FormatMappingValue(DriveIntent action, InputMappingMode mode)
        {
            if (mode == InputMappingMode.Keyboard)
                return KeyMapManager.FormatKey(_driveInput.KeyMap.GetKey(action));

            var axis = _driveInput.KeyMap.GetAxis(action);
            return _input.TryGetControllerDisplayProfile(out var profile)
                ? KeyMapManager.FormatAxis(axis, profile)
                : KeyMapManager.FormatAxis(axis, ControllerDisplayProfile.SemanticGamepad);
        }
    }
}





