using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Input
{
    public static class Touch
    {
        private const string LibraryName = SdlNativeLibrary.Name;

        public static ulong[] GetIds()
        {
            if (!Runtime.IsAvailable)
                return Array.Empty<ulong>();

            var pointer = SDL_GetTouchDevices(out var count);
            try
            {
                return Memory.ReadArray<ulong>(pointer, count);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                    SDL_Free(pointer);
            }
        }

        public static string? GetNameForId(ulong touchId)
        {
            return Utf8.FromNative(SDL_GetTouchDeviceName(touchId));
        }

        public static TouchDeviceType GetTypeForId(ulong touchId)
        {
            return SDL_GetTouchDeviceType(touchId);
        }

        public static Finger[] GetFingers(ulong touchId)
        {
            if (!Runtime.IsAvailable)
                return Array.Empty<Finger>();

            var pointer = SDL_GetTouchFingers(touchId, out var count);
            if (pointer == IntPtr.Zero || count <= 0)
                return Array.Empty<Finger>();

            try
            {
                var values = new Finger[count];
                for (var i = 0; i < count; i++)
                {
                    var fingerPointer = Marshal.ReadIntPtr(pointer, i * IntPtr.Size);
                    if (fingerPointer == IntPtr.Zero)
                        continue;

                    values[i] = Marshal.PtrToStructure<Finger>(fingerPointer);
                }

                return values;
            }
            finally
            {
                SDL_Free(pointer);
            }
        }

        [DllImport(LibraryName, EntryPoint = "SDL_GetTouchDevices", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetTouchDevices(out int count);

        [DllImport(LibraryName, EntryPoint = "SDL_GetTouchDeviceName", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetTouchDeviceName(ulong touchId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetTouchDeviceType", CallingConvention = CallingConvention.Cdecl)]
        private static extern TouchDeviceType SDL_GetTouchDeviceType(ulong touchId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetTouchFingers", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetTouchFingers(ulong touchId, out int count);

        [DllImport(LibraryName, EntryPoint = "SDL_free", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_Free(IntPtr memory);
    }
}
