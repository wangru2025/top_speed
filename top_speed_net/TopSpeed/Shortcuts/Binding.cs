using System;
using Key = TopSpeed.Input.InputKey;
using Gesture = TopSpeed.Input.GestureIntent;

namespace TopSpeed.Shortcuts
{
    internal readonly struct ShortcutBinding
    {
        public ShortcutBinding(
            string actionId,
            string displayName,
            string description,
            Key key,
            ShortcutModifiers modifiers,
            Gesture? gestureIntent = null)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("Shortcut action id is required.", nameof(actionId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Shortcut display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Shortcut description is required.", nameof(description));

            ActionId = actionId.Trim();
            DisplayName = displayName.Trim();
            Description = description.Trim();
            Key = key;
            Modifiers = modifiers;
            GestureIntent = gestureIntent;
        }

        public string ActionId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Key Key { get; }
        public ShortcutModifiers Modifiers { get; }
        public Gesture? GestureIntent { get; }
    }
}


