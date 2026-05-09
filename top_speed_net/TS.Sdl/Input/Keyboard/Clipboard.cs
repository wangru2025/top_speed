using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Input
{
    public static class Clipboard
    {
        private const string LibraryName = SdlNativeLibrary.Name;

        public static bool SetText(string? text)
        {
            if (!Runtime.IsAvailable)
                return false;

            return SDL_SetClipboardText(text ?? string.Empty);
        }

        public static string GetText()
        {
            if (!Runtime.IsAvailable)
                return string.Empty;

            var pointer = SDL_GetClipboardText();
            if (pointer == IntPtr.Zero)
                return string.Empty;

            try
            {
                return Utf8.FromNative(pointer) ?? string.Empty;
            }
            finally
            {
                SDL_Free(pointer);
            }
        }

        public static bool HasText()
        {
            if (!Runtime.IsAvailable)
                return false;

            return SDL_HasClipboardText();
        }

        [DllImport(LibraryName, EntryPoint = "SDL_SetClipboardText", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_SetClipboardText([MarshalAs(UnmanagedType.LPUTF8Str)] string text);

        [DllImport(LibraryName, EntryPoint = "SDL_GetClipboardText", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetClipboardText();

        [DllImport(LibraryName, EntryPoint = "SDL_HasClipboardText", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_HasClipboardText();

        [DllImport(LibraryName, EntryPoint = "SDL_free", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_Free(IntPtr memory);
    }
}
