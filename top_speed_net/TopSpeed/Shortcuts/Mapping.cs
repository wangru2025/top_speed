using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Speech;

namespace TopSpeed.Shortcuts
{
    internal sealed class ShortcutMappingHandler
    {
        private readonly IInputService _input;
        private readonly MenuManager _menu;
        private readonly DriveSettings _settings;
        private readonly SpeechService _speech;
        private readonly Action _saveSettings;

        private bool _isActive;
        private bool _needsInstruction;
        private string _groupId = string.Empty;
        private string _actionId = string.Empty;
        private string _displayName = string.Empty;

        public ShortcutMappingHandler(
            IInputService input,
            MenuManager menu,
            DriveSettings settings,
            SpeechService speech,
            Action saveSettings)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
        }

        public bool IsActive => _isActive;

        public void BeginMapping(string groupId, string actionId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return;
            if (string.IsNullOrWhiteSpace(actionId))
                return;

            _groupId = groupId.Trim();
            _actionId = actionId.Trim();
            _displayName = string.IsNullOrWhiteSpace(displayName) ? _actionId : displayName.Trim();
            _needsInstruction = true;
            _isActive = true;
        }

        public void Update()
        {
            if (!_isActive)
                return;

            if (_needsInstruction)
            {
                _needsInstruction = false;
                _speech.Speak(LocalizationService.Format(
                    LocalizationService.Mark("Press the new shortcut for {0}."),
                    LocalizationService.Translate(_displayName)));
            }

            if (_input.WasPressed(Key.Escape))
            {
                _isActive = false;
                _speech.Speak(LocalizationService.Mark("Shortcut mapping cancelled."));
                return;
            }

            for (var i = 1; i < 256; i++)
            {
                var key = (Key)i;
                if (!_input.WasPressed(key))
                    continue;

                if (ModifierKeys.TryGetGroup(key, out _))
                    continue;

                var modifiers = ShortcutModifiers.FromInput(_input);
                if (_menu.IsShortcutBindingInUse(_groupId, key, modifiers, _actionId))
                {
                    _speech.Speak(LocalizationService.Mark("That shortcut is already in use in this shortcut group."));
                    return;
                }

                try
                {
                    _menu.SetShortcutBinding(_actionId, key, modifiers);
                }
                catch (InvalidOperationException)
                {
                    _isActive = false;
                    _speech.Speak(LocalizationService.Mark("Unable to apply the new shortcut."));
                    return;
                }
                catch (ArgumentException)
                {
                    _isActive = false;
                    _speech.Speak(LocalizationService.Mark("Unable to apply the new shortcut."));
                    return;
                }

                _settings.ShortcutKeyBindings[_actionId] = key;
                _settings.ShortcutModifierBindings ??= new System.Collections.Generic.Dictionary<string, ShortcutModifiers>(StringComparer.Ordinal);
                _settings.ShortcutModifierBindings[_actionId] = modifiers;
                _saveSettings();
                _isActive = false;
                _speech.Speak(LocalizationService.Format(
                    LocalizationService.Mark("{0} set to {1}."),
                    LocalizationService.Translate(_displayName),
                    FormatBinding(key, modifiers)));
                return;
            }
        }

        private static string FormatBinding(Key key, ShortcutModifiers modifiers)
        {
            return ShortcutBindingText.Format(key, modifiers);
        }
    }
}




