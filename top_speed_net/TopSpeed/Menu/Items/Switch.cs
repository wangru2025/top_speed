using System;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal sealed class Switch : ToggleItem
    {
        private readonly string _valueOn;
        private readonly string _valueOff;

        public Switch(
            string text,
            string valueOn,
            string valueOff,
            Func<bool> getValue,
            Action<bool> setValue,
            Action<bool>? onChanged = null,
            MenuAction action = MenuAction.None,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false,
            string? hint = null,
            Func<string?>? hintProvider = null)
            : base(text, getValue, setValue, onChanged, action, nextMenuId, onActivate, suppressPostActivateAnnouncement, hint, hintProvider)
        {
            if (string.IsNullOrWhiteSpace(valueOn))
                throw new ArgumentException("valueOn must be provided.", nameof(valueOn));
            if (string.IsNullOrWhiteSpace(valueOff))
                throw new ArgumentException("valueOff must be provided.", nameof(valueOff));

            _valueOn = valueOn;
            _valueOff = valueOff;
        }

        public override string GetDisplayText()
        {
            return Describe(LocalizationService.Mark("switch"), GetValueLabel(GetValue()));
        }

        public override string? ActivateAndGetAnnouncement()
        {
            return Toggle(GetValueLabel);
        }

        private string GetValueLabel(bool value)
        {
            var raw = value ? _valueOn : _valueOff;
            return LocalizationService.Translate(raw);
        }
    }
}

