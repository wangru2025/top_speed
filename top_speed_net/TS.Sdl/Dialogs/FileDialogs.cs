using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Dialogs
{
    public static class FileDialogs
    {
        private const string LibraryName = SdlNativeLibrary.Name;

        public static void ShowOpenFile(Action<FileDialogResult> callback, IntPtr window, DialogFileFilter[]? filters = null, string? defaultLocation = null, bool allowMany = false)
        {
            Show(FileDialogType.OpenFile, callback, window, filters, defaultLocation, allowMany);
        }

        public static void ShowSaveFile(Action<FileDialogResult> callback, IntPtr window, DialogFileFilter[]? filters = null, string? defaultLocation = null)
        {
            Show(FileDialogType.SaveFile, callback, window, filters, defaultLocation, false);
        }

        public static void ShowOpenFolder(Action<FileDialogResult> callback, IntPtr window, string? defaultLocation = null, bool allowMany = false)
        {
            Show(FileDialogType.OpenFolder, callback, window, null, defaultLocation, allowMany);
        }

        private static void Show(FileDialogType type, Action<FileDialogResult> callback, IntPtr window, DialogFileFilter[]? filters, string? defaultLocation, bool allowMany)
        {
            if (!Runtime.IsAvailable)
            {
                callback(new FileDialogResult(Array.Empty<string>(), -1, false, "SDL3 library is not available."));
                return;
            }

            var request = new Request(callback, filters);
            var gcHandle = GCHandle.Alloc(request);
            var defaultLocationPointer = Utf8.ToNative(defaultLocation);
            var filterPointer = request.GetFilterPointer();

            try
            {
                switch (type)
                {
                    case FileDialogType.OpenFile:
                        SDL_ShowOpenFileDialog(Callback, GCHandle.ToIntPtr(gcHandle), window, filterPointer, request.FilterCount, defaultLocationPointer, allowMany);
                        break;
                    case FileDialogType.SaveFile:
                        SDL_ShowSaveFileDialog(Callback, GCHandle.ToIntPtr(gcHandle), window, filterPointer, request.FilterCount, defaultLocationPointer);
                        break;
                    case FileDialogType.OpenFolder:
                        SDL_ShowOpenFolderDialog(Callback, GCHandle.ToIntPtr(gcHandle), window, defaultLocationPointer, allowMany);
                        break;
                }
            }
            finally
            {
                if (defaultLocationPointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(defaultLocationPointer);
            }
        }

        private static void Callback(IntPtr userdata, IntPtr fileList, int filter)
        {
            var handle = GCHandle.FromIntPtr(userdata);
            if (!(handle.Target is Request request))
                return;

            try
            {
                if (fileList == IntPtr.Zero)
                {
                    request.Callback(new FileDialogResult(Array.Empty<string>(), filter, false, Runtime.GetError()));
                    return;
                }

                var paths = Utf8.ReadStringArray(fileList);
                request.Callback(new FileDialogResult(paths, filter, paths.Length == 0, null));
            }
            finally
            {
                request.Dispose();
                handle.Free();
            }
        }

        private sealed class Request : IDisposable
        {
            private readonly DialogFileFilter[] _filters;
            private GCHandle? _filtersHandle;

            public Request(Action<FileDialogResult> callback, DialogFileFilter[]? filters)
            {
                Callback = callback;
                _filters = filters ?? Array.Empty<DialogFileFilter>();
            }

            public Action<FileDialogResult> Callback { get; }
            public int FilterCount => _filters.Length;

            public IntPtr GetFilterPointer()
            {
                if (_filters.Length == 0)
                    return IntPtr.Zero;

                _filtersHandle = GCHandle.Alloc(_filters, GCHandleType.Pinned);
                return _filtersHandle.Value.AddrOfPinnedObject();
            }

            public void Dispose()
            {
                if (_filtersHandle.HasValue)
                {
                    _filtersHandle.Value.Free();
                    _filtersHandle = null;
                }

                for (var i = 0; i < _filters.Length; i++)
                    _filters[i].Dispose();
            }
        }

        [DllImport(LibraryName, EntryPoint = "SDL_ShowOpenFileDialog", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_ShowOpenFileDialog(NativeDialogFileCallback callback, IntPtr userdata, IntPtr window, IntPtr filters, int nfilters, IntPtr defaultLocation, [MarshalAs(UnmanagedType.I1)] bool allowMany);

        [DllImport(LibraryName, EntryPoint = "SDL_ShowSaveFileDialog", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_ShowSaveFileDialog(NativeDialogFileCallback callback, IntPtr userdata, IntPtr window, IntPtr filters, int nfilters, IntPtr defaultLocation);

        [DllImport(LibraryName, EntryPoint = "SDL_ShowOpenFolderDialog", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_ShowOpenFolderDialog(NativeDialogFileCallback callback, IntPtr userdata, IntPtr window, IntPtr defaultLocation, [MarshalAs(UnmanagedType.I1)] bool allowMany);
    }
}
