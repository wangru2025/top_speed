using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Platform
{
    public static class Android
    {
        private const string LibraryName = SdlNativeLibrary.Name;
        private static readonly NativePermissionCallback PermissionCallbackDelegate = PermissionCallback;

        public static bool RequestPermission(string permission, Action<AndroidPermissionResult> callback)
        {
            if (string.IsNullOrWhiteSpace(permission) || callback == null || !Runtime.IsAvailable)
                return false;

            var request = new PermissionRequest(callback);
            var handle = GCHandle.Alloc(request);
            try
            {
                var submitted = SDL_RequestAndroidPermission(permission, PermissionCallbackDelegate, GCHandle.ToIntPtr(handle));
                if (!submitted)
                    handle.Free();

                return submitted;
            }
            catch
            {
                if (handle.IsAllocated)
                    handle.Free();

                return false;
            }
        }

        private static void PermissionCallback(IntPtr userdata, IntPtr permission, [MarshalAs(UnmanagedType.I1)] bool granted)
        {
            var handle = GCHandle.FromIntPtr(userdata);
            if (!(handle.Target is PermissionRequest request))
            {
                if (handle.IsAllocated)
                    handle.Free();
                return;
            }

            try
            {
                try
                {
                    request.Callback(new AndroidPermissionResult(Utf8.FromNative(permission) ?? string.Empty, granted));
                }
                catch
                {
                    // Keep native callback boundary resilient to caller exceptions.
                }
            }
            finally
            {
                handle.Free();
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativePermissionCallback(IntPtr userdata, IntPtr permission, [MarshalAs(UnmanagedType.I1)] bool granted);

        [DllImport(LibraryName, EntryPoint = "SDL_RequestAndroidPermission", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_RequestAndroidPermission(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string permission,
            NativePermissionCallback callback,
            IntPtr userdata);

        private sealed class PermissionRequest
        {
            public PermissionRequest(Action<AndroidPermissionResult> callback)
            {
                Callback = callback;
            }

            public Action<AndroidPermissionResult> Callback { get; }
        }
    }

    public sealed class AndroidPermissionResult
    {
        public AndroidPermissionResult(string permission, bool granted)
        {
            Permission = permission;
            Granted = granted;
        }

        public string Permission { get; }
        public bool Granted { get; }
    }
}
