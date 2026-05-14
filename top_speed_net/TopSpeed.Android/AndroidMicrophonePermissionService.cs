using System;
using Android;
using Android.App;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using TopSpeed.Runtime;

namespace TopSpeed.Android
{
    internal sealed class AndroidMicrophonePermissionService : Java.Lang.Object, IMicrophonePermissionService
    {
        private const int RequestCode = 7023;
        private static readonly TimeSpan RequestCooldown = TimeSpan.FromSeconds(1);
        private readonly Activity _activity;
        private readonly object _sync = new object();
        private bool _requestInFlight;
        private DateTime _lastRequestUtc = DateTime.MinValue;

        public AndroidMicrophonePermissionService(Activity activity)
        {
            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        }

        public bool IsMicrophonePermissionGranted()
        {
            return ContextCompat.CheckSelfPermission(_activity, Manifest.Permission.RecordAudio) == Permission.Granted;
        }

        public bool EnsureMicrophonePermissionGranted()
        {
            if (IsMicrophonePermissionGranted())
            {
                lock (_sync)
                    _requestInFlight = false;
                return true;
            }

            lock (_sync)
            {
                if (_requestInFlight)
                    return false;

                var now = DateTime.UtcNow;
                if (now - _lastRequestUtc < RequestCooldown)
                    return false;

                _requestInFlight = true;
                _lastRequestUtc = now;
            }

            try
            {
                ActivityCompat.RequestPermissions(
                    _activity,
                    [Manifest.Permission.RecordAudio],
                    RequestCode);
            }
            catch
            {
                lock (_sync)
                    _requestInFlight = false;
            }

            return false;
        }

        public void HandlePermissionResult(int requestCode)
        {
            if (requestCode != RequestCode)
                return;

            lock (_sync)
                _requestInFlight = false;
        }
    }
}
