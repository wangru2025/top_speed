using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Shortcuts;

using TopSpeed.Localization;
namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private const string ShortcutGroupsMenuId = "options_controls_shortcuts";
        private const string ShortcutBindingsMenuId = "options_controls_shortcut_bindings";
        private const int ResetShortcutsYesChoiceId = 1;
        private const int ResetShortcutsNoChoiceId = 2;

        private string _activeShortcutGroupId = string.Empty;
        private string _activeShortcutGroupName = string.Empty;

        private MenuScreen BuildOptionsControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(
                    () => LocalizationService.Format(
                        LocalizationService.Mark("Select device: {0}"),
                        DeviceLabel(_settings.DeviceMode)),
                    MenuAction.None,
                    nextMenuId: "options_controls_device"),
                new CheckBox(LocalizationService.Mark("Force feedback"),
                    () => _settings.ForceFeedback,
                    value => _settingsActions.UpdateSetting(() => _settings.ForceFeedback = value),
                    hintProvider: HintToggleProvider(LocalizationService.Mark("Enables force feedback or vibration if your controller supports it."))),
                new RadioButton(LocalizationService.Mark("Progressive keyboard input"),
                    new[]
                    {
                        LocalizationService.Mark("Off"),
                        LocalizationService.Mark("Fastest (0.25 seconds)"),
                        LocalizationService.Mark("Fast (0.50 seconds)"),
                        LocalizationService.Mark("Moderate (0.75 seconds)"),
                        LocalizationService.Mark("Slowest (1.00 second)")
                    },
                    () => (int)_settings.KeyboardProgressiveRate,
                    value => _settingsActions.UpdateSetting(() => _settings.KeyboardProgressiveRate = (KeyboardProgressiveRate)value),
                    hintProvider: HintForPlatformProvider(
                        LocalizationService.Mark("When enabled, throttle, brake, and steering ramp in over time instead of jumping instantly to full value."),
                        LocalizationService.Mark("Press LEFT or RIGHT to change."),
                        LocalizationService.Mark("Swipe left or right with two fingers to change."))),
                new MenuItem(LocalizationService.Mark("Map keyboard keys"), MenuAction.None, nextMenuId: "options_controls_keyboard"),
                new MenuItem(LocalizationService.Mark("Map controller keys"), MenuAction.None, nextMenuId: "options_controls_controller"),
                new MenuItem(LocalizationService.Mark("Map menu shortcuts"),
                    MenuAction.None,
                    nextMenuId: ShortcutGroupsMenuId,
                    onActivate: RebuildShortcutGroupsMenu)
            };

            if (InteractionHints.IsAndroidPlatform())
            {
                items.Insert(3,
                    new CheckBox(
                        LocalizationService.Mark("Use motion sensors for steering"),
                        () => _settings.AndroidUseMotionSteering,
                        value => _settingsActions.UpdateSetting(() => _settings.AndroidUseMotionSteering = value),
                        hintProvider: HintToggleProvider(LocalizationService.Mark("When checked, steering uses Android motion sensors instead of drag gestures."))));
            }
            return BackMenu("options_controls", items);
        }

        private MenuScreen BuildOptionsControlsDeviceMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Keyboard"), MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Keyboard)),
                new MenuItem(LocalizationService.Mark("Controller"), MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Controller)),
                new MenuItem(LocalizationService.Mark("Both"), MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Both))
            };
            return BackMenu("options_controls_device", items, LocalizationService.Mark("Select input device"));
        }

        private MenuScreen BuildOptionsControlsKeyboardMenu()
        {
            return BackMenu("options_controls_keyboard", BuildMappingItems(InputMappingMode.Keyboard));
        }

        private MenuScreen BuildOptionsControlsControllerMenu()
        {
            var items = new List<MenuItem>
            {
                new RadioButton(LocalizationService.Mark("Throttle pedal direction"),
                    new[]
                    {
                        LocalizationService.Mark("Auto"),
                        LocalizationService.Mark("Normal"),
                        LocalizationService.Mark("Inverted")
                    },
                    () => (int)_settings.ControllerThrottleInvertMode,
                    value => _settingsActions.UpdateSetting(() => _settings.ControllerThrottleInvertMode = (PedalInvertMode)value),
                    hintProvider: HintAdjustProvider(LocalizationService.Mark("Auto detects wheel pedal direction from resting position."))),
                new RadioButton(LocalizationService.Mark("Brake pedal direction"),
                    new[]
                    {
                        LocalizationService.Mark("Auto"),
                        LocalizationService.Mark("Normal"),
                        LocalizationService.Mark("Inverted")
                    },
                    () => (int)_settings.ControllerBrakeInvertMode,
                    value => _settingsActions.UpdateSetting(() => _settings.ControllerBrakeInvertMode = (PedalInvertMode)value),
                    hintProvider: HintAdjustProvider(LocalizationService.Mark("Auto detects wheel pedal direction from resting position."))),
                new RadioButton(LocalizationService.Mark("Clutch pedal direction"),
                    new[]
                    {
                        LocalizationService.Mark("Auto"),
                        LocalizationService.Mark("Normal"),
                        LocalizationService.Mark("Inverted")
                    },
                    () => (int)_settings.ControllerClutchInvertMode,
                    value => _settingsActions.UpdateSetting(() => _settings.ControllerClutchInvertMode = (PedalInvertMode)value),
                    hintProvider: HintAdjustProvider(LocalizationService.Mark("Auto detects wheel pedal direction from resting position."))),
                new RadioButton(LocalizationService.Mark("Steering dead zone"),
                    new[]
                    {
                        LocalizationService.Mark("Default (1 degree)"),
                        LocalizationService.Mark("2 degrees"),
                        LocalizationService.Mark("3 degrees"),
                        LocalizationService.Mark("4 degrees"),
                        LocalizationService.Mark("5 degrees")
                    },
                    () =>
                    {
                        var deadZone = _settings.ControllerSteeringDeadZone;
                        if (deadZone < 1 || deadZone > 5)
                            deadZone = 1;
                        return deadZone - 1;
                    },
                    value =>
                    {
                        var deadZone = value + 1;
                        if (deadZone < 1 || deadZone > 5)
                            deadZone = 1;
                        _settingsActions.UpdateSetting(() => _settings.ControllerSteeringDeadZone = deadZone);
                    },
                    hintProvider: HintAdjustProvider(LocalizationService.Mark("Sets how much small steering movement is ignored around center. Default is 1 degree.")))
            };

            items.AddRange(BuildMappingItems(InputMappingMode.Controller));
            return BackMenu("options_controls_controller", items);
        }

        private MenuScreen BuildOptionsControlsShortcutGroupsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("Global shortcuts"), MenuAction.None)
            };
            return BackMenu(ShortcutGroupsMenuId, items, title: string.Empty);
        }

        private MenuScreen BuildOptionsControlsShortcutBindingsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(LocalizationService.Mark("No shortcuts in this group."), MenuAction.None)
            };
            return BackMenu(ShortcutBindingsMenuId, items, title: string.Empty);
        }

        private List<MenuItem> BuildMappingItems(InputMappingMode mode)
        {
            var items = new List<MenuItem>();
            foreach (var action in _driveInput.KeyMap.Actions)
            {
                var definition = action;
                items.Add(new MenuItem(
                    () => FormatLabelValue(
                        LocalizationService.Translate(definition.Label),
                        _mapping.FormatMappingValue(definition.Action, mode)),
                    MenuAction.None,
                    onActivate: () => _mapping.BeginMapping(mode, definition.Action)));
            }

            items.Add(BuildResetMappingsItem(mode));
            return items;
        }

        private MenuItem BuildResetMappingsItem(InputMappingMode mode)
        {
            return mode == InputMappingMode.Keyboard
                ? new MenuItem(
                    LocalizationService.Mark("Reset all keyboard mappings"),
                    MenuAction.None,
                    onActivate: () => _mapping.ResetMappings(InputMappingMode.Keyboard),
                    hint: LocalizationService.Mark("Restore all keyboard bindings to their default values."))
                : new MenuItem(
                    LocalizationService.Mark("Reset all controller mappings"),
                    MenuAction.None,
                    onActivate: () => _mapping.ResetMappings(InputMappingMode.Controller),
                    hint: LocalizationService.Mark("Restore all controller bindings to their default values."));
        }

        private void RebuildShortcutGroupsMenu()
        {
            var groups = _menu.GetShortcutGroups();
            var items = new List<MenuItem>();

            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                items.Add(new MenuItem(
                    group.Name,
                    MenuAction.None,
                    onActivate: () => OpenShortcutGroup(group)));
            }

            items.Add(new MenuItem(
                LocalizationService.Mark("Reset all menu shortcuts to defaults"),
                MenuAction.None,
                onActivate: ShowResetAllShortcutBindingsDialog,
                hint: LocalizationService.Mark("Restore every menu shortcut binding to its default value.")));

            _menu.UpdateItems(ShortcutGroupsMenuId, items, preserveSelection: true);
        }

        private void OpenShortcutGroup(ShortcutGroup group)
        {
            _activeShortcutGroupId = group.Id;
            _activeShortcutGroupName = group.Name;
            if (!RebuildShortcutBindingsMenu())
            {
                _ui.SpeakMessage(LocalizationService.Format(
                    LocalizationService.Mark("{0} has no shortcuts."),
                    LocalizationService.Translate(group.Name)));
                return;
            }

            _menu.Push(ShortcutBindingsMenuId);
        }

        private bool RebuildShortcutBindingsMenu()
        {
            var items = new List<MenuItem>();
            if (string.IsNullOrWhiteSpace(_activeShortcutGroupId))
            {
                items.Add(new MenuItem(LocalizationService.Mark("No shortcut group selected."), MenuAction.None));
                _menu.UpdateItems(ShortcutBindingsMenuId, items, preserveSelection: true);
                return false;
            }

            var bindings = _menu.GetShortcutBindings(_activeShortcutGroupId);

            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                var actionId = binding.ActionId;
                var displayName = binding.DisplayName;
                var description = binding.Description;
                items.Add(new MenuItem(
                    () => FormatLabelValue(
                        LocalizationService.Translate(displayName),
                        GetShortcutKeyText(actionId, binding.Key)),
                    MenuAction.None,
                    onActivate: () => _mapping.BeginShortcutMapping(_activeShortcutGroupId, actionId, displayName),
                    hint: description));
            }

            if (bindings.Count > 0)
            {
                items.Add(new MenuItem(
                    LocalizationService.Mark("Reset shortcuts in this menu"),
                    MenuAction.None,
                    onActivate: ShowResetActiveShortcutGroupDialog,
                    hint: LocalizationService.Mark("Restore only the shortcuts in this menu to their default values.")));
            }

            if (items.Count == 0)
                return false;

            _menu.UpdateItems(ShortcutBindingsMenuId, items, preserveSelection: true);
            return true;
        }

        private void ShowResetAllShortcutBindingsDialog()
        {
            _ui.ShowChoiceDialog(
                LocalizationService.Mark("Reset all menu shortcuts?"),
                LocalizationService.Mark("This will restore every menu shortcut binding to its default value."),
                BuildResetShortcutChoiceItems(
                    LocalizationService.Mark("Yes"),
                    LocalizationService.Mark("No")),
                cancelable: true,
                cancelLabel: LocalizationService.Mark("Cancel"),
                onResult: HandleResetAllShortcutBindingsDialog);
        }

        private void HandleResetAllShortcutBindingsDialog(ChoiceDialogResult result)
        {
            if (result.IsCanceled || result.ChoiceId == ResetShortcutsNoChoiceId)
                return;
            if (result.ChoiceId != ResetShortcutsYesChoiceId)
                return;

            _menu.ResetShortcutBindings();
            _settingsActions.UpdateSetting(() =>
            {
                _settings.ShortcutKeyBindings = new Dictionary<string, InputKey>(StringComparer.Ordinal);
                _settings.ShortcutModifierBindings = new Dictionary<string, ShortcutModifiers>(StringComparer.Ordinal);
            });

            RebuildShortcutBindingsMenu();
            RebuildShortcutGroupsMenu();
            _ui.SpeakMessage(LocalizationService.Mark("All menu shortcuts were reset to defaults."));
        }

        private void ShowResetActiveShortcutGroupDialog()
        {
            if (string.IsNullOrWhiteSpace(_activeShortcutGroupId))
                return;

            var groupName = GetActiveShortcutGroupName();
            _ui.ShowChoiceDialog(
                LocalizationService.Mark("Reset shortcuts in this menu?"),
                LocalizationService.Format(
                    LocalizationService.Mark("This will restore all shortcuts in {0} to their default values."),
                    groupName),
                BuildResetShortcutChoiceItems(
                    LocalizationService.Mark("Yes"),
                    LocalizationService.Mark("No")),
                cancelable: true,
                cancelLabel: LocalizationService.Mark("Cancel"),
                onResult: HandleResetActiveShortcutGroupDialog);
        }

        private void HandleResetActiveShortcutGroupDialog(ChoiceDialogResult result)
        {
            if (result.IsCanceled || result.ChoiceId == ResetShortcutsNoChoiceId)
                return;
            if (result.ChoiceId != ResetShortcutsYesChoiceId)
                return;
            if (string.IsNullOrWhiteSpace(_activeShortcutGroupId))
                return;

            var bindings = _menu.GetShortcutBindings(_activeShortcutGroupId);
            if (bindings.Count == 0)
            {
                _ui.SpeakMessage(LocalizationService.Mark("This shortcut menu has no bindings to reset."));
                return;
            }

            if (!_menu.ResetShortcutBindingsInGroup(_activeShortcutGroupId))
                return;

            _settingsActions.UpdateSetting(() =>
            {
                _settings.ShortcutKeyBindings ??= new Dictionary<string, InputKey>(StringComparer.Ordinal);
                _settings.ShortcutModifierBindings ??= new Dictionary<string, ShortcutModifiers>(StringComparer.Ordinal);

                for (var i = 0; i < bindings.Count; i++)
                {
                    var actionId = bindings[i].ActionId;
                    _settings.ShortcutKeyBindings.Remove(actionId);
                    _settings.ShortcutModifierBindings.Remove(actionId);
                }
            });

            RebuildShortcutBindingsMenu();
            _ui.SpeakMessage(LocalizationService.Format(
                LocalizationService.Mark("Shortcuts in {0} were reset to defaults."),
                GetActiveShortcutGroupName()));
        }

        private string GetActiveShortcutGroupName()
        {
            var groupName = _activeShortcutGroupName;
            if (string.IsNullOrWhiteSpace(groupName))
                groupName = LocalizationService.Mark("this menu");

            return LocalizationService.Translate(groupName);
        }

        private static IReadOnlyDictionary<int, string> BuildResetShortcutChoiceItems(string yesLabel, string noLabel)
        {
            return new Dictionary<int, string>
            {
                [ResetShortcutsYesChoiceId] = yesLabel,
                [ResetShortcutsNoChoiceId] = noLabel
            };
        }

        private string GetShortcutKeyText(string actionId, InputKey fallbackKey)
        {
            if (_menu.TryGetShortcutBinding(actionId, out var binding))
                return FormatShortcutKey(binding.Key, binding.Modifiers);
            return FormatShortcutKey(fallbackKey, ShortcutModifiers.None);
        }

        private static string FormatShortcutKey(InputKey key, ShortcutModifiers modifiers)
        {
            return ShortcutBindingText.Format(key, modifiers);
        }

        private static string FormatLabelValue(string label, string value)
        {
            return string.Concat(label, ": ", value);
        }
    }
}


