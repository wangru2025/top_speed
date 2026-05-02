using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidX.Core.Content;
using TopSpeed.Runtime;

namespace TopSpeed.Android;

internal sealed class AndroidUpdatePackageInstaller : Java.Lang.Object, IUpdatePackageInstaller
{
    private readonly Activity _activity;

    public AndroidUpdatePackageInstaller(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    public bool TryInstallPackage(string packagePath, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            errorMessage = "Update package path is empty.";
            return false;
        }

        var fullPath = Path.GetFullPath(packagePath);
        if (!File.Exists(fullPath))
        {
            errorMessage = $"Update package was not found: {fullPath}";
            return false;
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".apk", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Android updater expects an APK package.";
            return false;
        }

        try
        {
            var packageManager = _activity.PackageManager;
            if (packageManager == null)
            {
                errorMessage = "Package manager is unavailable.";
                return false;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O && !packageManager.CanRequestPackageInstalls())
            {
                var permissionIntent = new Intent(Settings.ActionManageUnknownAppSources);
                permissionIntent.SetData(global::Android.Net.Uri.Parse("package:" + _activity.PackageName));
                permissionIntent.AddFlags(ActivityFlags.NewTask);
                _activity.StartActivity(permissionIntent);
                errorMessage = "Allow 'Install unknown apps' for TopSpeed, then run update install again.";
                return false;
            }

            var apkFile = new Java.IO.File(fullPath);
            var authority = _activity.PackageName + ".fileprovider";
            var apkUri = FileProvider.GetUriForFile(_activity, authority, apkFile);
            if (apkUri == null)
            {
                errorMessage = "Failed to create package URI for installer.";
                return false;
            }

            #pragma warning disable CA1422
            var installIntent = new Intent(Intent.ActionInstallPackage);
            #pragma warning restore CA1422
            installIntent.SetData(apkUri);
            installIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
            installIntent.PutExtra(Intent.ExtraReturnResult, false);
            _activity.StartActivity(installIntent);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
