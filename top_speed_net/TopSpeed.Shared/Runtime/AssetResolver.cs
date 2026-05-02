using System;
using System.Runtime.InteropServices;

namespace TopSpeed.Runtime
{
    public static class RuntimeAssetResolver
    {
        public static string DetectClientRuntimeAssetTag()
        {
            return DetectClientRuntimeAssetTag(
                GetRuntimeIdentifier(),
                RuntimeInformation.ProcessArchitecture,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
        }

        public static string DetectServerRuntimeAssetTag()
        {
            return DetectServerRuntimeAssetTag(
                GetRuntimeIdentifier(),
                RuntimeInformation.ProcessArchitecture,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
        }

        public static string ResolveExecutableFileName(string stem)
        {
            return ResolveExecutableFileName(
                stem,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }

        public static string DetectClientRuntimeAssetTag(
            string? runtimeIdentifier,
            Architecture architecture,
            bool isWindows,
            bool isLinux,
            bool isMacOs)
        {
            var rid = NormalizeRuntimeIdentifier(runtimeIdentifier);
            if (rid.Contains("android-arm64"))
                return "android-arm64";
            if (rid.Contains("android-arm"))
                return "android-arm";
            if (rid.Contains("android"))
                return "android";
            if (rid.Contains("win"))
                return "windows-x64";
            if ((rid.Contains("osx") || rid.Contains("mac")) && rid.Contains("arm64"))
                return "mac-arm64";
            if (rid.Contains("osx") || rid.Contains("mac"))
                return "mac-x64";
            if (rid.Contains("linux"))
                return "linux-x64";

            if (isWindows)
                return "windows-x64";
            if (isMacOs)
            {
                return architecture == Architecture.Arm64
                    ? "mac-arm64"
                    : "mac-x64";
            }
            if (isLinux)
                return "linux-x64";

            throw new PlatformNotSupportedException(
                $"Unsupported client update runtime. RuntimeIdentifier='{runtimeIdentifier}', Architecture='{architecture}'.");
        }

        public static string DetectServerRuntimeAssetTag(
            string? runtimeIdentifier,
            Architecture architecture,
            bool isWindows,
            bool isLinux,
            bool isMacOs)
        {
            var rid = NormalizeRuntimeIdentifier(runtimeIdentifier);
            if (rid.Contains("linux-musl-x64"))
                return "linux-musl-x64";
            if (rid.Contains("linux-musl-arm64"))
                return "linux-musl-arm64";
            if (rid.Contains("linux-x64"))
                return "linux-x64";
            if (rid.Contains("linux-arm64"))
                return "linux-arm64";
            if (rid.Contains("linux-arm"))
                return "linux-arm32";
            if (rid.Contains("linux-x86"))
                return "linux-x86-fdd";
            if ((rid.Contains("osx") || rid.Contains("mac")) && rid.Contains("arm64"))
                return "mac-arm64";
            if (rid.Contains("osx") || rid.Contains("mac"))
                return "mac-x64";
            if (rid.Contains("win"))
                return "win-x64";

            if (isWindows)
                return "win-x64";
            if (isMacOs)
            {
                return architecture == Architecture.Arm64
                    ? "mac-arm64"
                    : "mac-x64";
            }

            if (isLinux)
            {
                return architecture switch
                {
                    Architecture.X64 => "linux-x64",
                    Architecture.Arm64 => "linux-arm64",
                    Architecture.Arm => "linux-arm32",
                    Architecture.X86 => "linux-x86-fdd",
                    _ => throw new PlatformNotSupportedException(
                        $"Unsupported server Linux architecture '{architecture}'.")
                };
            }

            throw new PlatformNotSupportedException(
                $"Unsupported server update runtime. RuntimeIdentifier='{runtimeIdentifier}', Architecture='{architecture}'.");
        }

        public static string ResolveExecutableFileName(string stem, bool isWindows)
        {
            if (string.IsNullOrWhiteSpace(stem))
                throw new ArgumentException("Executable stem is required.", nameof(stem));

            return isWindows
                ? stem + ".exe"
                : stem;
        }

        private static string NormalizeRuntimeIdentifier(string? runtimeIdentifier)
        {
            return (runtimeIdentifier ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string? GetRuntimeIdentifier()
        {
            return AppContext.GetData("RUNTIME_IDENTIFIER") as string;
        }
    }
}
