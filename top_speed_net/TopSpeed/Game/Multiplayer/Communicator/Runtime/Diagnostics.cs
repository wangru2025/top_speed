using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    // Lightweight voice-chat diagnostic logger. Opt-in via the
    // TOPSPEED_VOICE_DEBUG environment variable (any non-empty value enables it).
    // When enabled, logs are appended to a "voice_debug.log" file in the user's
    // home/TopSpeed data directory (or %TEMP% as a last resort), with timestamps
    // and a single short message per line. This is the only way a Windows
    // GUI client can show "did the mic actually capture anything?" diagnostics
    // to the user — there is no console attached and the game has no logging
    // infrastructure of its own.
    internal static class VoiceDebug
    {
        private static readonly bool Enabled = ResolveEnabled();
        private static readonly string LogPath = ResolveLogPath();
        private static readonly object Sync = new object();

        public static bool IsEnabled => Enabled;

        public static void Log(string message)
        {
            if (!Enabled)
                return;

            try
            {
                var line = string.Format(
                    CultureInfo.InvariantCulture,
                    "[{0:yyyy-MM-dd HH:mm:ss.fff}][tid={1}] {2}{3}",
                    DateTime.UtcNow,
                    Thread.CurrentThread.ManagedThreadId,
                    message,
                    Environment.NewLine);

                lock (Sync)
                {
                    File.AppendAllText(LogPath, line);
                }
            }
            catch
            {
                // Diagnostic logging must never crash the runtime.
            }
        }

        private static bool ResolveEnabled()
        {
            try
            {
                var raw = Environment.GetEnvironmentVariable("TOPSPEED_VOICE_DEBUG");
                return !string.IsNullOrWhiteSpace(raw)
                    && !string.Equals(raw, "0", StringComparison.Ordinal)
                    && !string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveLogPath()
        {
            try
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(home))
                    home = Path.GetTempPath();

                var dir = Path.Combine(home, ".topspeed");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "voice_debug.log");
            }
            catch
            {
                return Path.Combine(Path.GetTempPath(), "topspeed_voice_debug.log");
            }
        }
    }
}
