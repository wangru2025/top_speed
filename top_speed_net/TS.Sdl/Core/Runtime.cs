using System;
using System.Runtime.InteropServices;
using TS.Sdl.Events;
using TS.Sdl.Interop;

namespace TS.Sdl
{
    public static class Runtime
    {
        private const string LibraryName = SdlNativeLibrary.Name;

        public static bool IsAvailable => Library.EnsureLoaded();

        public static bool Init(InitFlags flags)
        {
            if (!IsAvailable)
                return false;

            return SDL_Init(flags);
        }

        public static void SetMainReady()
        {
            if (!IsAvailable)
                return;

            SDL_SetMainReady();
        }

        public static bool InitSubSystem(InitFlags flags)
        {
            if (!IsAvailable)
                return false;

            return SDL_InitSubSystem(flags);
        }

        public static void QuitSubSystem(InitFlags flags)
        {
            if (!IsAvailable)
                return;

            SDL_QuitSubSystem(flags);
        }

        public static InitFlags WasInit(InitFlags flags)
        {
            if (!IsAvailable)
                return 0;

            return SDL_WasInit(flags);
        }

        public static void Quit()
        {
            if (!IsAvailable)
                return;

            SDL_Quit();
        }

        public static string GetError()
        {
            if (!IsAvailable)
                return Library.LastError;

            return Utf8.FromNative(SDL_GetError()) ?? string.Empty;
        }

        public static void ClearError()
        {
            if (!IsAvailable)
                return;

            SDL_ClearError();
        }

        public static void PumpEvents()
        {
            if (!IsAvailable)
                return;

            SDL_PumpEvents();
        }

        public static bool PollEvent(out Event value)
        {
            value = default;
            if (!IsAvailable)
                return false;

            return SDL_PollEvent(out value);
        }

        public static bool WaitEventTimeout(int timeoutMs)
        {
            if (!IsAvailable)
                return false;

            return SDL_WaitEventTimeout(IntPtr.Zero, timeoutMs);
        }

        public static bool IsMainThread()
        {
            if (!IsAvailable)
                return false;

            return SDL_IsMainThread();
        }

        public static ulong GetTicksNs()
        {
            if (!IsAvailable)
                return 0;

            return SDL_GetTicksNS();
        }

        [DllImport(LibraryName, EntryPoint = "SDL_Init", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_Init(InitFlags flags);

        [DllImport(LibraryName, EntryPoint = "SDL_SetMainReady", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_SetMainReady();

        [DllImport(LibraryName, EntryPoint = "SDL_InitSubSystem", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_InitSubSystem(InitFlags flags);

        [DllImport(LibraryName, EntryPoint = "SDL_QuitSubSystem", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_QuitSubSystem(InitFlags flags);

        [DllImport(LibraryName, EntryPoint = "SDL_WasInit", CallingConvention = CallingConvention.Cdecl)]
        private static extern InitFlags SDL_WasInit(InitFlags flags);

        [DllImport(LibraryName, EntryPoint = "SDL_Quit", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_Quit();

        [DllImport(LibraryName, EntryPoint = "SDL_GetError", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetError();

        [DllImport(LibraryName, EntryPoint = "SDL_ClearError", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_ClearError();

        [DllImport(LibraryName, EntryPoint = "SDL_PumpEvents", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_PumpEvents();

        [DllImport(LibraryName, EntryPoint = "SDL_PollEvent", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_PollEvent(out Event value);

        [DllImport(LibraryName, EntryPoint = "SDL_WaitEventTimeout", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_WaitEventTimeout(IntPtr eventPtr, int timeoutMs);

        [DllImport(LibraryName, EntryPoint = "SDL_IsMainThread", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_IsMainThread();

        [DllImport(LibraryName, EntryPoint = "SDL_GetTicksNS", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong SDL_GetTicksNS();
    }
}
