namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager
    {
        public void ShowRoot(string id, string? openingAnnouncement = null, int? preferredSelectionIndex = null, bool autoFocus = true)
        {
            foreach (var existingScreen in _stack)
                existingScreen.CancelPendingHint();
            _stack.Clear();
            var screen = GetScreen(id);
            screen.ResetSelection(preferredSelectionIndex, autoFocus);
            screen.Initialize();
            _stack.Push(screen);
            screen.QueueTitleAnnouncement(openingAnnouncement);
        }

        public void Push(string id, string? openingAnnouncement = null, int? preferredSelectionIndex = null, bool autoFocus = true)
        {
            if (_stack.Count > 0)
                _stack.Peek().CancelPendingHint();
            var screen = GetScreen(id);
            screen.ResetSelection(preferredSelectionIndex, autoFocus);
            screen.Initialize();
            _stack.Push(screen);
            screen.QueueTitleAnnouncement(openingAnnouncement);
        }

        public void ReplaceTop(string id, string? openingAnnouncement = null, int? preferredSelectionIndex = null, bool autoFocus = true)
        {
            if (_stack.Count == 0)
            {
                ShowRoot(id, openingAnnouncement, preferredSelectionIndex, autoFocus);
                return;
            }

            _stack.Peek().CancelPendingHint();
            _stack.Pop();
            var screen = GetScreen(id);
            screen.ResetSelection(preferredSelectionIndex, autoFocus);
            screen.Initialize();
            _stack.Push(screen);
            screen.QueueTitleAnnouncement(openingAnnouncement);
        }

        public void PopToPrevious(bool announceTitle = true)
        {
            if (_stack.Count <= 1)
                return;

            _stack.Peek().CancelPendingHint();
            _stack.Pop();
            if (announceTitle)
                _stack.Peek().QueueTitleAnnouncement();
        }

        public bool HasActiveMenu => _stack.Count > 0;
        public bool CanPop => _stack.Count > 1;
        public string? CurrentId => _stack.Count > 0 ? _stack.Peek().Id : null;
    }
}

