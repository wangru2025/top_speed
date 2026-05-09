using System;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal class MenuItem
    {
        private readonly string _text;
        private readonly Func<string>? _textProvider;
        private readonly Func<string?>? _hintProvider;
        private readonly MenuItemAction[] _actions;
        public string? Hint { get; }

        public string Text => _text;
        public MenuAction Action { get; }
        public string? NextMenuId { get; }
        public Action? OnActivate { get; }
        public bool SuppressPostActivateAnnouncement { get; }
        public MenuItemFlags Flags { get; }
        public bool Hidden { get; set; }
        public bool IsCloseItem => (Flags & MenuItemFlags.Close) != 0;
        public bool IsHidden => Hidden;
        public bool HasActions => _actions.Length > 0;
        public int ActionCount => _actions.Length;

        public MenuItem(
            string text,
            MenuAction action,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false,
            string? hint = null,
            MenuItemFlags flags = MenuItemFlags.None,
            Func<string?>? hintProvider = null,
            params MenuItemAction[] actions)
        {
            _text = text;
            _textProvider = null;
            Action = action;
            NextMenuId = nextMenuId;
            OnActivate = onActivate;
            SuppressPostActivateAnnouncement = suppressPostActivateAnnouncement;
            Hint = hint;
            _hintProvider = hintProvider;
            Flags = flags;
            Hidden = (flags & MenuItemFlags.Hidden) != 0;
            _actions = actions ?? Array.Empty<MenuItemAction>();
        }

        public MenuItem(
            Func<string> textProvider,
            MenuAction action,
            string? nextMenuId = null,
            Action? onActivate = null,
            bool suppressPostActivateAnnouncement = false,
            string? hint = null,
            MenuItemFlags flags = MenuItemFlags.None,
            Func<string?>? hintProvider = null,
            params MenuItemAction[] actions)
        {
            _text = string.Empty;
            _textProvider = textProvider ?? throw new ArgumentNullException(nameof(textProvider));
            Action = action;
            NextMenuId = nextMenuId;
            OnActivate = onActivate;
            SuppressPostActivateAnnouncement = suppressPostActivateAnnouncement;
            Hint = hint;
            _hintProvider = hintProvider;
            Flags = flags;
            Hidden = (flags & MenuItemFlags.Hidden) != 0;
            _actions = actions ?? Array.Empty<MenuItemAction>();
        }

        public virtual string GetDisplayText()
        {
            var text = _textProvider?.Invoke() ?? _text;
            return LocalizationService.Translate(text);
        }

        public virtual string? ActivateAndGetAnnouncement()
        {
            OnActivate?.Invoke();
            return null;
        }

        public virtual bool Adjust(MenuAdjustAction action, out string? announcement)
        {
            announcement = null;
            return false;
        }

        public bool TryActivateAction(int actionIndex)
        {
            if (actionIndex < 0 || actionIndex >= _actions.Length)
                return false;

            _actions[actionIndex].Activate();
            return true;
        }

        public bool TryGetActionLabel(int actionIndex, out string label)
        {
            label = string.Empty;
            if (actionIndex < 0 || actionIndex >= _actions.Length)
                return false;

            var rawLabel = _actions[actionIndex].Label ?? string.Empty;
            label = LocalizationService.Translate(rawLabel);
            return true;
        }

        public virtual string? GetHintText()
        {
            var translatedHint = _hintProvider != null
                ? _hintProvider()
                : string.IsNullOrWhiteSpace(Hint)
                    ? null
                    : LocalizationService.Translate(Hint);

            if (HasActions)
            {
                var actionsHint = InteractionHints.ForPlatform(
                    LocalizationService.Mark("Actions available."),
                    LocalizationService.Mark("Press right arrow to view."),
                    LocalizationService.Mark("Swipe left or right with two fingers to view."));
                if (string.IsNullOrWhiteSpace(translatedHint))
                    return actionsHint;
                return $"{translatedHint} {actionsHint}";
            }

            return translatedHint;
        }

        protected string GetBaseText()
        {
            var text = _textProvider?.Invoke() ?? _text;
            return LocalizationService.Translate(text);
        }
    }
}

