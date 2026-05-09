using System;

namespace TopSpeed.Menu
{
    internal abstract class ToggleItem : MenuItem
    {
        private readonly Func<bool> _getValue;
        private readonly Action<bool> _setValue;
        private readonly Action<bool>? _onChanged;

        protected ToggleItem(
            string text,
            Func<bool> getValue,
            Action<bool> setValue,
            Action<bool>? onChanged = null,
            MenuAction action = MenuAction.None,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false,
            string? hint = null,
            Func<string?>? hintProvider = null)
            : base(text, action, nextMenuId, onActivate, suppressPostActivateAnnouncement, hint, hintProvider: hintProvider)
        {
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
            _setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
            _onChanged = onChanged;
        }

        protected bool GetValue()
        {
            return _getValue();
        }

        protected string Describe(string kind, string value, bool separated = true)
        {
            return ItemText.Compose(GetBaseText(), kind, value, separated);
        }

        protected string Toggle(Func<bool, string> format)
        {
            var newValue = !_getValue();
            _setValue(newValue);
            _onChanged?.Invoke(newValue);
            base.ActivateAndGetAnnouncement();
            return format(newValue);
        }
    }
}
