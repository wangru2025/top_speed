using System.Collections.Generic;
using TopSpeed.Input;
using Key = TopSpeed.Input.InputKey;

namespace TopSpeed.Shortcuts
{
    internal static class ShortcutBindingText
    {
        public static string Format(Key key, ShortcutModifiers modifiers)
        {
            var parts = new List<string>(4);
            if (modifiers.Control)
                parts.Add(ModifierKeys.GetDisplayName(ModifierKeyGroup.Control));
            if (modifiers.Shift)
                parts.Add(ModifierKeys.GetDisplayName(ModifierKeyGroup.Shift));
            if (modifiers.Alt)
                parts.Add(ModifierKeys.GetDisplayName(ModifierKeyGroup.Alt));

            parts.Add(InputDisplayText.Key(key));
            return string.Join(" + ", parts);
        }
    }
}
