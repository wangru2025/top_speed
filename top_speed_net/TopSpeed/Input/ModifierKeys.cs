using TopSpeed.Localization;
using Key = TopSpeed.Input.InputKey;

namespace TopSpeed.Input
{
    internal enum ModifierKeyGroup
    {
        Shift,
        Control,
        Alt
    }

    internal enum ModifierKeySelection
    {
        Left = 1,
        Right = 2,
        Both = 3
    }

    internal static class ModifierKeys
    {
        public static bool TryGetGroup(Key key, out ModifierKeyGroup group)
        {
            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                case Key.BothShift:
                    group = ModifierKeyGroup.Shift;
                    return true;
                case Key.LeftControl:
                case Key.RightControl:
                case Key.BothControl:
                    group = ModifierKeyGroup.Control;
                    return true;
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.BothAlt:
                    group = ModifierKeyGroup.Alt;
                    return true;
                default:
                    group = default;
                    return false;
            }
        }

        public static bool Conflicts(Key left, Key right)
        {
            if (left == right)
                return true;
            if (!TryGetGroup(left, out var leftGroup) || !TryGetGroup(right, out var rightGroup))
                return false;
            if (leftGroup != rightGroup)
                return false;

            return left == GetBothKey(leftGroup) || right == GetBothKey(rightGroup);
        }

        public static bool IsBothKey(Key key)
        {
            return key == Key.BothShift
                || key == Key.BothControl
                || key == Key.BothAlt;
        }

        public static Key GetLeftKey(ModifierKeyGroup group)
        {
            return group switch
            {
                ModifierKeyGroup.Shift => Key.LeftShift,
                ModifierKeyGroup.Control => Key.LeftControl,
                ModifierKeyGroup.Alt => Key.LeftAlt,
                _ => Key.Unknown
            };
        }

        public static Key GetRightKey(ModifierKeyGroup group)
        {
            return group switch
            {
                ModifierKeyGroup.Shift => Key.RightShift,
                ModifierKeyGroup.Control => Key.RightControl,
                ModifierKeyGroup.Alt => Key.RightAlt,
                _ => Key.Unknown
            };
        }

        public static Key GetBothKey(ModifierKeyGroup group)
        {
            return group switch
            {
                ModifierKeyGroup.Shift => Key.BothShift,
                ModifierKeyGroup.Control => Key.BothControl,
                ModifierKeyGroup.Alt => Key.BothAlt,
                _ => Key.Unknown
            };
        }

        public static Key GetKey(ModifierKeyGroup group, ModifierKeySelection selection)
        {
            return selection switch
            {
                ModifierKeySelection.Left => GetLeftKey(group),
                ModifierKeySelection.Right => GetRightKey(group),
                ModifierKeySelection.Both => GetBothKey(group),
                _ => Key.Unknown
            };
        }

        public static string GetDisplayName(ModifierKeyGroup group)
        {
            return group switch
            {
                ModifierKeyGroup.Shift => LocalizationService.Translate(LocalizationService.Mark("Shift")),
                ModifierKeyGroup.Control => LocalizationService.Translate(LocalizationService.Mark("Control")),
                ModifierKeyGroup.Alt => LocalizationService.Translate(LocalizationService.Mark("Alt")),
                _ => LocalizationService.Translate(LocalizationService.Mark("Modifier"))
            };
        }
    }
}
