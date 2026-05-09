using System;
using System.Runtime.InteropServices;

namespace TS.Sdl.Dialogs
{
    public static class MessageBoxes
    {
        private const string LibraryName = SdlNativeLibrary.Name;

        public static bool ShowSimple(MessageBoxFlags flags, string title, string message, IntPtr window)
        {
            if (!Runtime.IsAvailable)
                return false;

            return SDL_ShowSimpleMessageBox(flags, title ?? string.Empty, message ?? string.Empty, window);
        }

        [DllImport(LibraryName, EntryPoint = "SDL_ShowSimpleMessageBox", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_ShowSimpleMessageBox(
            MessageBoxFlags flags,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string title,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string message,
            IntPtr window);
    }
}
