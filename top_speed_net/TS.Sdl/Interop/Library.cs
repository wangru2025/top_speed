using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TS.Sdl.Interop
{
    internal static class Library
    {
        private const int RtldNow = 2;
        private const int RtldGlobal = 0x100;
        private static readonly OSPlatform AndroidPlatform = OSPlatform.Create("ANDROID");
        private static readonly OSPlatform IosPlatform = OSPlatform.Create("IOS");
        private static readonly OSPlatform MacCatalystPlatform = OSPlatform.Create("MACCATALYST");

        private static readonly object Sync = new object();
        private static bool _attempted;
        private static bool _loaded;
        private static string _lastError = "SDL3 library has not been loaded yet.";

        public static string LastError
        {
            get
            {
                lock (Sync)
                {
                    return _lastError;
                }
            }
        }

        public static bool EnsureLoaded()
        {
            lock (Sync)
            {
                if (_attempted)
                    return _loaded;

                _attempted = true;
                var failures = new System.Collections.Generic.List<string>();
                foreach (var candidate in GetCandidates())
                {
                    if (TryLoad(candidate.Path, candidate.RequireFile, out var error))
                    {
                        _loaded = true;
                        _lastError = string.Empty;
                        break;
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                        failures.Add($"{candidate.Display}: {error}");
                }

                if (!_loaded)
                    _lastError = failures.Count > 0
                        ? $"SDL3 could not be loaded. Attempts: {string.Join(" | ", failures)}"
                        : "SDL3 could not be loaded.";

                return _loaded;
            }
        }

        private static Candidate[] GetCandidates()
        {
            var baseDir = AppContext.BaseDirectory;
            var fileName = GetLibraryFileName();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new[]
                {
                    new Candidate(Path.Combine(baseDir, "lib", fileName), true, Path.Combine("lib", fileName)),
                    new Candidate(Path.Combine(baseDir, fileName), true, fileName),
                    new Candidate(fileName, false, $"system:{fileName}")
                };
            }

            if (IsIosLike())
            {
                return new[]
                {
                    new Candidate(Path.Combine(baseDir, "Frameworks", "SDL3.framework", "SDL3"), true, Path.Combine("Frameworks", "SDL3.framework", "SDL3")),
                    new Candidate(Path.Combine(baseDir, "SDL3.framework", "SDL3"), true, Path.Combine("SDL3.framework", "SDL3")),
                    new Candidate("SDL3.framework/SDL3", false, "system:SDL3.framework/SDL3"),
                    new Candidate("SDL3", false, "system:SDL3")
                };
            }

            var shortName = GetShortLibraryName(fileName);
            return new[]
            {
                new Candidate(Path.Combine(baseDir, "lib", fileName), true, Path.Combine("lib", fileName)),
                new Candidate(Path.Combine(baseDir, fileName), true, fileName),
                new Candidate(fileName, false, $"system:{fileName}"),
                new Candidate(shortName, false, $"system:{shortName}")
            };
        }

        private static string GetLibraryFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "SDL3.dll";
            if (IsIosLike())
                return "SDL3.framework/SDL3";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "libSDL3.dylib";
            if (IsAndroid())
                return "libSDL3.so";
            return "libSDL3.so";
        }

        private static string GetShortLibraryName(string libraryFileName)
        {
            if (libraryFileName.StartsWith("lib", StringComparison.OrdinalIgnoreCase) &&
                libraryFileName.EndsWith(".so", StringComparison.OrdinalIgnoreCase))
            {
                return libraryFileName.Substring(3, libraryFileName.Length - 6);
            }

            return libraryFileName;
        }

        private static bool TryLoad(string path, bool requireFile, out string error)
        {
            if (requireFile && !File.Exists(path))
            {
                error = "not found";
                return false;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var handle = LoadLibrary(path);
                    if (handle != IntPtr.Zero)
                    {
                        error = string.Empty;
                        return true;
                    }

                    error = $"LoadLibrary failed ({Marshal.GetLastWin32Error()})";
                    return false;
                }

                var dlHandle = Dlopen(path, RtldNow | RtldGlobal);
                if (dlHandle != IntPtr.Zero)
                {
                    error = string.Empty;
                    return true;
                }

                error = GetDlError() ?? "dlopen failed";
                return false;
            }
            catch (Exception ex)
            {
                error = $"{ex.GetType().Name}: {ex.Message}";
                return false;
            }
        }

        private static string? GetDlError()
        {
            var pointer = IsApplePlatform()
                ? DlerrorMac()
                : IsAndroid()
                    ? DlerrorAndroid()
                    : DlerrorLinux();
            return pointer == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(pointer);
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("libdl.so.2", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlopenLinux(string fileName, int flags);

        [DllImport("libdl.so", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlopenAndroid(string fileName, int flags);

        [DllImport("libSystem.B.dylib", EntryPoint = "dlopen", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlopenMac(string fileName, int flags);

        [DllImport("libdl.so.2", EntryPoint = "dlerror", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlerrorLinux();

        [DllImport("libdl.so", EntryPoint = "dlerror", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlerrorAndroid();

        [DllImport("libSystem.B.dylib", EntryPoint = "dlerror", CharSet = CharSet.Ansi)]
        private static extern IntPtr DlerrorMac();

        private static IntPtr Dlopen(string fileName, int flags)
        {
            return IsApplePlatform()
                ? DlopenMac(fileName, flags)
                : IsAndroid()
                    ? DlopenAndroid(fileName, flags)
                    : DlopenLinux(fileName, flags);
        }

        private static bool IsAndroid()
        {
            return RuntimeInformation.IsOSPlatform(AndroidPlatform);
        }

        private static bool IsIosLike()
        {
            return RuntimeInformation.IsOSPlatform(IosPlatform) || RuntimeInformation.IsOSPlatform(MacCatalystPlatform);
        }

        private static bool IsApplePlatform()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || IsIosLike();
        }

        private readonly struct Candidate
        {
            public Candidate(string path, bool requireFile, string display)
            {
                Path = path;
                RequireFile = requireFile;
                Display = display;
            }

            public string Path { get; }
            public bool RequireFile { get; }
            public string Display { get; }
        }
    }
}
