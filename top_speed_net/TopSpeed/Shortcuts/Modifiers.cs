using System;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;

namespace TopSpeed.Shortcuts
{
    internal readonly struct ShortcutModifiers : IEquatable<ShortcutModifiers>
    {
        public ShortcutModifiers(bool shift, bool control, bool alt)
        {
            Shift = shift;
            Control = control;
            Alt = alt;
        }

        public bool Shift { get; }
        public bool Control { get; }
        public bool Alt { get; }

        public bool IsEmpty => !Shift && !Control && !Alt;

        public static ShortcutModifiers None => default;

        public static ShortcutModifiers FromInput(IInputService input)
        {
            if (input == null)
                return None;

            return new ShortcutModifiers(
                IsModifierDown(input, ModifierKeyGroup.Shift),
                IsModifierDown(input, ModifierKeyGroup.Control),
                IsModifierDown(input, ModifierKeyGroup.Alt));
        }

        public bool MatchesInput(IInputService input)
        {
            if (input == null)
                return false;

            return IsModifierDown(input, ModifierKeyGroup.Shift) == Shift
                && IsModifierDown(input, ModifierKeyGroup.Control) == Control
                && IsModifierDown(input, ModifierKeyGroup.Alt) == Alt;
        }

        public bool Equals(ShortcutModifiers other)
        {
            return Shift == other.Shift
                && Control == other.Control
                && Alt == other.Alt;
        }

        public override bool Equals(object? obj)
        {
            return obj is ShortcutModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Shift ? 1 : 0;
                hash = (hash * 397) ^ (Control ? 1 : 0);
                hash = (hash * 397) ^ (Alt ? 1 : 0);
                return hash;
            }
        }

        private static bool IsModifierDown(IInputService input, ModifierKeyGroup group)
        {
            var left = input.IsDown(ModifierKeys.GetLeftKey(group));
            var right = input.IsDown(ModifierKeys.GetRightKey(group));
            var both = input.IsDown(ModifierKeys.GetBothKey(group));
            return left || right || both;
        }
    }
}

