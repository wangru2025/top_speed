using System;
using System.Runtime.InteropServices;

namespace TS.Sdl.Video
{
    public static class Window
    {
        private const string LibraryName = SdlNativeLibrary.Name;

        public static IntPtr Create(string title, int width, int height, WindowFlags flags = WindowFlags.None)
        {
            if (!Runtime.IsAvailable)
                return IntPtr.Zero;

            return SDL_CreateWindow(title ?? string.Empty, width, height, flags);
        }

        public static void Destroy(IntPtr window)
        {
            if (!Runtime.IsAvailable || window == IntPtr.Zero)
                return;

            SDL_DestroyWindow(window);
        }

        public static uint GetId(IntPtr window)
        {
            if (!Runtime.IsAvailable || window == IntPtr.Zero)
                return 0;

            return SDL_GetWindowID(window);
        }

        public static bool SetTitle(IntPtr window, string title)
        {
            if (!Runtime.IsAvailable || window == IntPtr.Zero)
                return false;

            return SDL_SetWindowTitle(window, title ?? string.Empty);
        }

        public static bool Show(IntPtr window)
        {
            if (!Runtime.IsAvailable || window == IntPtr.Zero)
                return false;

            return SDL_ShowWindow(window);
        }

        public static uint GetDisplayForWindow(IntPtr window)
        {
            if (!Runtime.IsAvailable || window == IntPtr.Zero)
                return 0;

            return SDL_GetDisplayForWindow(window);
        }

        public static uint GetPrimaryDisplay()
        {
            if (!Runtime.IsAvailable)
                return 0;

            return SDL_GetPrimaryDisplay();
        }

        public static DisplayOrientation GetCurrentDisplayOrientation(uint displayId)
        {
            if (!Runtime.IsAvailable || displayId == 0)
                return DisplayOrientation.Unknown;

            return SDL_GetCurrentDisplayOrientation(displayId);
        }

        [DllImport(LibraryName, EntryPoint = "SDL_CreateWindow", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)] string title, int w, int h, WindowFlags flags);

        [DllImport(LibraryName, EntryPoint = "SDL_DestroyWindow", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_DestroyWindow(IntPtr window);

        [DllImport(LibraryName, EntryPoint = "SDL_GetWindowID", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetWindowID(IntPtr window);

        [DllImport(LibraryName, EntryPoint = "SDL_SetWindowTitle", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_SetWindowTitle(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);

        [DllImport(LibraryName, EntryPoint = "SDL_ShowWindow", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_ShowWindow(IntPtr window);

        [DllImport(LibraryName, EntryPoint = "SDL_GetDisplayForWindow", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetDisplayForWindow(IntPtr window);

        [DllImport(LibraryName, EntryPoint = "SDL_GetPrimaryDisplay", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetPrimaryDisplay();

        [DllImport(LibraryName, EntryPoint = "SDL_GetCurrentDisplayOrientation", CallingConvention = CallingConvention.Cdecl)]
        private static extern DisplayOrientation SDL_GetCurrentDisplayOrientation(uint displayId);
    }
}
