using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;
using TopSpeed.Localization;
using TopSpeed.Shortcuts;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyInput(DriveSettings settings, SettingsInputDocument input, List<SettingsIssue> issues)
        {
            if (input.ForceFeedback.HasValue)
                settings.ForceFeedback = input.ForceFeedback.Value;

            settings.KeyboardProgressiveRate = ReadEnum(input.KeyboardProgressiveRate, settings.KeyboardProgressiveRate, "input.keyboardProgressiveRate", issues);
            settings.DeviceMode = ReadEnum(input.DeviceMode, settings.DeviceMode, "input.deviceMode", issues);
            if (input.AndroidUseMotionSteering.HasValue)
                settings.AndroidUseMotionSteering = input.AndroidUseMotionSteering.Value;

            if (input.Keyboard == null)
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    "input.keyboard",
                    LocalizationService.Mark("Keyboard bindings section is missing. Defaults were used for keyboard bindings.")));
            else
                ApplyKeyboard(settings, input.Keyboard, issues);

            ApplyMenuShortcuts(settings, input.MenuShortcuts, issues);

            if (input.Controller == null)
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    "input.controller",
                    LocalizationService.Mark("Controller bindings section is missing. Defaults were used for controller bindings.")));
            else
                ApplyController(settings, input.Controller, issues);
        }

        private static void ApplyKeyboard(DriveSettings settings, SettingsKeyboardDocument keyboard, List<SettingsIssue> issues)
        {
            if (keyboard.Bindings == null)
                return;

            for (var i = 0; i < keyboard.Bindings.Count; i++)
            {
                var binding = keyboard.Bindings[i];
                if (binding == null)
                    continue;

                var fieldPrefix = $"input.keyboard.bindings[{i}]";
                if (!TryReadDriveIntent(binding.Intent, fieldPrefix + ".intent", issues, out var intent))
                    continue;
                if (intent == DriveIntent.Steering)
                    continue;

                var fallback = settings.GetKeyboardBinding(intent);
                var key = ReadKey(binding.Key, fallback, fieldPrefix + ".key", issues);
                settings.SetKeyboardBinding(intent, key);
            }
        }

        private static void ApplyController(DriveSettings settings, SettingsControllerDocument controller, List<SettingsIssue> issues)
        {
            if (controller.Bindings != null)
            {
                for (var i = 0; i < controller.Bindings.Count; i++)
                {
                    var binding = controller.Bindings[i];
                    if (binding == null)
                        continue;

                    var fieldPrefix = $"input.controller.bindings[{i}]";
                    if (!TryReadDriveIntent(binding.Intent, fieldPrefix + ".intent", issues, out var intent))
                        continue;
                    if (intent == DriveIntent.Steering)
                        continue;

                    var fallback = settings.GetControllerBinding(intent);
                    var axis = ReadController(binding.Axis, fallback, fieldPrefix + ".axis", issues);
                    settings.SetControllerBinding(intent, axis);
                }
            }

            settings.ControllerThrottleInvertMode = ReadEnum(controller.ThrottleInvertMode, settings.ControllerThrottleInvertMode, "input.controller.throttleInvertMode", issues);
            settings.ControllerBrakeInvertMode = ReadEnum(controller.BrakeInvertMode, settings.ControllerBrakeInvertMode, "input.controller.brakeInvertMode", issues);
            settings.ControllerClutchInvertMode = ReadEnum(controller.ClutchInvertMode, settings.ControllerClutchInvertMode, "input.controller.clutchInvertMode", issues);
            settings.ControllerSteeringDeadZone = ClampInt(controller.SteeringDeadZone, settings.ControllerSteeringDeadZone, 1, 5, "input.controller.steeringDeadZone", issues);

            if (controller.Center == null)
                return;

            var center = settings.ControllerCenter;
            if (controller.Center.X.HasValue) center.X = controller.Center.X.Value;
            if (controller.Center.Y.HasValue) center.Y = controller.Center.Y.Value;
            if (controller.Center.Z.HasValue) center.Z = controller.Center.Z.Value;
            if (controller.Center.Rx.HasValue) center.Rx = controller.Center.Rx.Value;
            if (controller.Center.Ry.HasValue) center.Ry = controller.Center.Ry.Value;
            if (controller.Center.Rz.HasValue) center.Rz = controller.Center.Rz.Value;
            if (controller.Center.Slider1.HasValue) center.Slider1 = controller.Center.Slider1.Value;
            if (controller.Center.Slider2.HasValue) center.Slider2 = controller.Center.Slider2.Value;
            settings.ControllerCenter = center;
        }

        private static bool TryReadDriveIntent(string? value, string field, List<SettingsIssue> issues, out DriveIntent intent)
        {
            intent = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    field,
                    LocalizationService.Mark("Binding intent is missing and was ignored.")));
                return false;
            }

            var token = value!.Trim();
            if (!Enum.TryParse<DriveIntent>(token, ignoreCase: true, out intent) || !Enum.IsDefined(typeof(DriveIntent), intent))
            {
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    field,
                    LocalizationService.Format(
                        LocalizationService.Mark("The key {0} has invalid value {1} and was ignored."),
                        field,
                        value)));
                return false;
            }

            return true;
        }

        private static void ApplyMenuShortcuts(DriveSettings settings, SettingsMenuShortcutsDocument? menuShortcuts, List<SettingsIssue> issues)
        {
            settings.ShortcutKeyBindings = new Dictionary<string, Key>(System.StringComparer.Ordinal);
            settings.ShortcutModifierBindings = new Dictionary<string, ShortcutModifiers>(System.StringComparer.Ordinal);
            if (menuShortcuts?.Bindings == null)
                return;

            for (var i = 0; i < menuShortcuts.Bindings.Count; i++)
            {
                var binding = menuShortcuts.Bindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.Id))
                {
                    issues.Add(new SettingsIssue(
                        SettingsIssueSeverity.Warning,
                        $"input.menuShortcuts.bindings[{i}].id",
                        LocalizationService.Mark("Shortcut binding id is missing and was ignored.")));
                    continue;
                }

                if (!binding.Key.HasValue)
                    continue;

                var keyValue = binding.Key.Value;
                if (!System.Enum.IsDefined(typeof(Key), keyValue) || keyValue < 0)
                {
                    issues.Add(new SettingsIssue(
                        SettingsIssueSeverity.Warning,
                        $"input.menuShortcuts.bindings[{i}].key",
                        LocalizationService.Format(
                            LocalizationService.Mark("The key input.menuShortcuts.bindings[{0}].key has invalid value {1} and was ignored."),
                            i,
                            keyValue)));
                    continue;
                }

                var actionId = binding.Id!.Trim();
                settings.ShortcutKeyBindings[actionId] = (Key)keyValue;
                settings.ShortcutModifierBindings[actionId] = new ShortcutModifiers(
                    shift: binding.Shift ?? false,
                    control: binding.Control ?? false,
                    alt: binding.Alt ?? false);
            }
        }
    }
}



