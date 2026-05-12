#if WINDOWS
using System.Runtime.InteropServices;
using System.Threading;

namespace TopSpeed.Runtime
{
    // Windows system-timer resolution is global to the process and affects
    // every Thread.Sleep / Wait the app issues. The default ~15.6 ms scheduler
    // tick is too coarse for the race game loop (8 ms / 125 fps) and for the
    // multiplayer poll thread (5 ms), so we request 1 ms resolution once for
    // the lifetime of the process. timeBeginPeriod is reference-counted by
    // the OS; pairing it with timeEndPeriod on shutdown keeps things tidy if
    // another component also requested high resolution.
    internal static class WindowsTimerResolution
    {
        private const uint HighResolutionMilliseconds = 1;
        private static int _activated;

        public static void EnablePermanentHighResolution()
        {
            if (Interlocked.Exchange(ref _activated, 1) != 0)
                return;

            try
            {
                timeBeginPeriod(HighResolutionMilliseconds);
            }
            catch
            {
                Interlocked.Exchange(ref _activated, 0);
            }
        }

        public static void DisablePermanentHighResolution()
        {
            if (Interlocked.Exchange(ref _activated, 0) != 1)
                return;

            try
            {
                timeEndPeriod(HighResolutionMilliseconds);
            }
            catch
            {
            }
        }

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint timeEndPeriod(uint uPeriod);
    }
}
#else
namespace TopSpeed.Runtime
{
    // Non-Windows platforms do not have a global timer-resolution knob; the
    // requests are no-ops so callers can use the same API everywhere.
    internal static class WindowsTimerResolution
    {
        public static void EnablePermanentHighResolution()
        {
        }

        public static void DisablePermanentHighResolution()
        {
        }
    }
}
#endif
