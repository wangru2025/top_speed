using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using SteamAudio;
using TopSpeed.Speech.Prism;

namespace TopSpeed.Runtime
{
    internal static class NativeLibraryBootstrap
    {
        private static bool _initialized;
        private static readonly List<IntPtr> _loadedHandles = new List<IntPtr>();
        private static readonly Dictionary<string, IntPtr> _loadedLibraries = new Dictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<Assembly> _resolvedAssemblies = new HashSet<Assembly>();

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            var libDirectory = Path.Combine(AppContext.BaseDirectory, "lib");
            var hasLibDirectory = Directory.Exists(libDirectory);

            InstallResolver(typeof(IPL).Assembly, hasLibDirectory ? libDirectory : null);
            InstallResolver(typeof(Native).Assembly, hasLibDirectory ? libDirectory : null);

            if (hasLibDirectory)
                TryPreload(libDirectory, GetPhononCandidates());
        }
        private static void InstallResolver(Assembly assembly, string? libDirectory)
        {
            if (_resolvedAssemblies.Contains(assembly))
                return;

            try
            {
                NativeLibrary.SetDllImportResolver(
                    assembly,
                    (libraryName, _, _) => ResolveLibrary(libraryName, libDirectory));
                _resolvedAssemblies.Add(assembly);
            }
            catch
            {
                // Resolver may already be set by another bootstrap path.
            }
        }

        private static IntPtr ResolveLibrary(string libraryName, string? libDirectory)
        {
            if (IsPhononLibraryName(libraryName))
                return TryLoadFirst(libDirectory, GetPhononCandidates());

            if (IsPrismLibraryName(libraryName))
                return TryLoadFirst(libDirectory, GetPrismCandidates());

            return IntPtr.Zero;
        }

        private static void TryPreload(string libDirectory, IReadOnlyList<string> candidates)
        {
            TryLoadFromDirectory(libDirectory, candidates);
        }

        private static IntPtr TryLoadFirst(string? libDirectory, IReadOnlyList<string> candidates)
        {
            if (!string.IsNullOrWhiteSpace(libDirectory))
            {
                var fromDirectory = TryLoadFromDirectory(libDirectory, candidates);
                if (fromDirectory != IntPtr.Zero)
                    return fromDirectory;
            }

            return TryLoadByName(candidates);
        }

        private static IntPtr TryLoadFromDirectory(string libDirectory, IReadOnlyList<string> candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var relative = candidates[i];
                var absolutePath = Path.GetFullPath(Path.Combine(libDirectory, relative));
                if (!File.Exists(absolutePath))
                    continue;

                if (_loadedLibraries.TryGetValue(absolutePath, out var existing))
                    return existing;

                try
                {
                    var handle = NativeLibrary.Load(absolutePath);
                    _loadedHandles.Add(handle);
                    _loadedLibraries[absolutePath] = handle;
                    return handle;
                }
                catch
                {
                    // Try next candidate.
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr TryLoadByName(IReadOnlyList<string> candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var name = candidates[i];
                if (_loadedLibraries.TryGetValue(name, out var existing))
                    return existing;

                try
                {
                    var handle = NativeLibrary.Load(name);
                    _loadedHandles.Add(handle);
                    _loadedLibraries[name] = handle;
                    return handle;
                }
                catch
                {
                    // Try next candidate.
                }
            }

            return IntPtr.Zero;
        }

        private static bool IsPhononLibraryName(string? libraryName)
        {
            if (string.IsNullOrWhiteSpace(libraryName))
                return false;
            return libraryName.IndexOf("phonon", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPrismLibraryName(string? libraryName)
        {
            if (string.IsNullOrWhiteSpace(libraryName))
                return false;
            return libraryName.IndexOf("prism", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IReadOnlyList<string> GetPhononCandidates()
        {
            if (IsIOS())
                return new[] { "phonon", "__Internal" };
            if (IsAndroid())
                return new[] { "libphonon.so", "phonon" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new[] { "phonon.dll", "phonon" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new[]
                {
                    "libphonon.dylib",
                    "phonon",
                    Path.Combine("phonon.bundle", "Contents", "MacOS", "phonon")
                };
            }

            return new[] { "libphonon.so", "phonon" };
        }

        private static IReadOnlyList<string> GetPrismCandidates()
        {
            if (IsIOS())
                return new[] { "__Internal", "prism" };
            if (IsAndroid())
                return new[] { "libprism.so", "prism" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new[] { "prism.dll", "prism" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new[] { "libprism.dylib", "prism" };
            return new[] { "libprism.so", "prism" };
        }

        private static bool IsIOS()
        {
#if NET10_0_OR_GREATER
            return OperatingSystem.IsIOS();
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS"));
#endif
        }

        private static bool IsAndroid()
        {
#if NET10_0_OR_GREATER
            return OperatingSystem.IsAndroid();
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
#endif
        }
    }
}
