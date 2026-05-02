using System;
using System.Runtime.InteropServices;
using TopSpeed.Localization;

namespace TopSpeed.Menu
{
    internal static class InteractionHints
    {
        private static readonly OSPlatform Android = OSPlatform.Create("ANDROID");

        public static bool IsAndroidPlatform()
        {
            if (OperatingSystem.IsAndroid())
                return true;
            if (RuntimeInformation.IsOSPlatform(Android))
                return true;
            if (RuntimeInformation.RuntimeIdentifier.StartsWith("android", StringComparison.OrdinalIgnoreCase))
                return true;
            return Type.GetType("Android.OS.Build, Mono.Android", throwOnError: false) != null;
        }

        public static bool IsTouchPlatform()
        {
            var explicitTouchHints = Environment.GetEnvironmentVariable("TOPSPEED_TOUCH_HINTS");
            if (string.Equals(explicitTouchHints, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(explicitTouchHints, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsAndroidPlatform())
                return true;
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANDROID_ROOT")))
                return true;
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANDROID_DATA")))
                return true;

            return RuntimeInformation.IsOSPlatform(Android);
        }

        public static string ForPlatform(string desktopText, string touchText)
        {
            return IsTouchPlatform() ? touchText : desktopText;
        }

        public static string ForPlatform(string hint, string desktopControl, string touchControl)
        {
            var text = LocalizationService.Translate(hint).Trim();
            var control = LocalizationService.Translate(ForPlatform(desktopControl, touchControl)).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return control;
            if (string.IsNullOrWhiteSpace(control))
                return text;

            return text + " " + control;
        }
    }
}
