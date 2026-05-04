using System.Runtime.InteropServices;
using Eto;
using Eto.Forms;

namespace TopSpeed.Windowing.Eto
{
    internal static class ApplicationFactory
    {
        public static Application GetOrCreate()
        {
            return Application.Instance ?? new Application(ResolvePlatform());
        }

        private static string ResolvePlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Platforms.Mac64;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Platforms.Gtk;

            return Platforms.Gtk;
        }
    }
}
