using System;
using Key = TopSpeed.Input.InputKey;
using Gesture = TopSpeed.Input.GestureIntent;

namespace TopSpeed.Shortcuts
{
    internal sealed class ShortcutAction
    {
        private readonly Action _onTrigger;
        private readonly Func<bool>? _canExecute;

        public ShortcutAction(
            string id,
            string displayName,
            string description,
            Key key,
            ShortcutModifiers modifiers,
            Action onTrigger,
            Func<bool>? canExecute = null,
            Gesture? gestureIntent = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Shortcut action id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Shortcut display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Shortcut description is required.", nameof(description));

            Id = id.Trim();
            DisplayName = displayName.Trim();
            Description = description.Trim();
            Key = key;
            DefaultKey = key;
            Modifiers = modifiers;
            DefaultModifiers = modifiers;
            GestureIntent = gestureIntent;
            DefaultGestureIntent = gestureIntent;
            _onTrigger = onTrigger ?? throw new ArgumentNullException(nameof(onTrigger));
            _canExecute = canExecute;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Key Key { get; private set; }
        public Key DefaultKey { get; }
        public ShortcutModifiers Modifiers { get; private set; }
        public ShortcutModifiers DefaultModifiers { get; }
        public Gesture? GestureIntent { get; private set; }
        public Gesture? DefaultGestureIntent { get; }

        public void SetBinding(Key key, ShortcutModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public void ResetKey()
        {
            Key = DefaultKey;
            Modifiers = DefaultModifiers;
            GestureIntent = DefaultGestureIntent;
        }

        public bool CanExecute()
        {
            return _canExecute == null || _canExecute();
        }

        public void Trigger()
        {
            _onTrigger();
        }
    }
}


