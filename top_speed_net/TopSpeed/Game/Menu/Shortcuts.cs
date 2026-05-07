using System;
using System.Collections.Generic;
using Key = TopSpeed.Input.InputKey;
using TopSpeed.Shortcuts;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ApplySavedShortcutBindings()
        {
            if (_settings.ShortcutKeyBindings == null || _settings.ShortcutKeyBindings.Count == 0)
                return;
            _settings.ShortcutModifierBindings ??= new Dictionary<string, ShortcutModifiers>(StringComparer.Ordinal);

            var appliedBindings = new Dictionary<string, Key>(StringComparer.Ordinal);
            var appliedModifiers = new Dictionary<string, ShortcutModifiers>(StringComparer.Ordinal);
            foreach (var pair in _settings.ShortcutKeyBindings)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;
                if (!_menu.TryGetShortcutBinding(pair.Key, out _))
                    continue;

                var modifiers = ShortcutModifiers.None;
                if (_settings.ShortcutModifierBindings.TryGetValue(pair.Key, out var savedModifiers))
                {
                    modifiers = savedModifiers;
                }

                try
                {
                    _menu.SetShortcutBinding(pair.Key, pair.Value, modifiers);
                    appliedBindings[pair.Key] = pair.Value;
                    appliedModifiers[pair.Key] = modifiers;
                }
                catch (System.ArgumentException)
                {
                }
                catch (System.InvalidOperationException)
                {
                }
            }

            _settings.ShortcutKeyBindings = appliedBindings;
            _settings.ShortcutModifierBindings = appliedModifiers;
        }
    }
}


