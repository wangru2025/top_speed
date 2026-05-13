namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        private bool TryHandleLetterNavigation(in MenuLetterPress letterPress)
        {
            if (!letterPress.HasLetter || letterPress.ReservedByShortcut)
                return false;
            if (_items.Count == 0)
                return false;

            var letter = letterPress.Letter;
            var start = _index == NoSelection ? 0 : (_index + 1) % _items.Count;
            for (var i = 0; i < _items.Count; i++)
            {
                var idx = (start + i) % _items.Count;
                if (!MenuInputUtil.ItemStartsWithLetter(_items[idx], letter))
                    continue;

                _activeActionIndex = NoSelection;
                MoveToIndex(idx);
                return true;
            }

            return false;
        }
    }
}



