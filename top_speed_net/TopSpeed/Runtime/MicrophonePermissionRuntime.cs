using System;

namespace TopSpeed.Runtime
{
    public static class MicrophonePermissionRuntime
    {
        private static readonly object Sync = new object();
        private static IMicrophonePermissionService? _service;

        public static void SetService(IMicrophonePermissionService? service)
        {
            IMicrophonePermissionService? previous;
            lock (Sync)
            {
                previous = _service;
                _service = service;
            }

            if (previous is IDisposable disposable)
                disposable.Dispose();
        }

        public static bool IsPermissionGranted()
        {
            IMicrophonePermissionService? service;
            lock (Sync)
                service = _service;

            return service?.IsMicrophonePermissionGranted() ?? true;
        }

        public static bool EnsurePermissionGranted()
        {
            IMicrophonePermissionService? service;
            lock (Sync)
                service = _service;

            return service?.EnsureMicrophonePermissionGranted() ?? true;
        }
    }
}
