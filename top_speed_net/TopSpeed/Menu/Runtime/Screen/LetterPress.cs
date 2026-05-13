namespace TopSpeed.Menu
{
    internal readonly struct MenuLetterPress
    {
        public static MenuLetterPress None => default;

        public MenuLetterPress(char letter, bool reservedByShortcut)
        {
            Letter = letter;
            ReservedByShortcut = reservedByShortcut;
        }

        public char Letter { get; }
        public bool ReservedByShortcut { get; }
        public bool HasLetter => Letter != '\0';
    }
}
