using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Webkit;
using AndroidX.Core.Content;
using TopSpeed.Runtime;

namespace TopSpeed.Android;

internal sealed class AndroidDocumentOpener : Java.Lang.Object, IDocumentOpener
{
    private readonly Activity _activity;

    public AndroidDocumentOpener(Activity activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    public bool TryOpenDocument(string path, string contentType, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Document path is empty.";
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            errorMessage = $"Document was not found: {fullPath}";
            return false;
        }

        try
        {
            var documentFile = new Java.IO.File(fullPath);
            var authority = _activity.PackageName + ".fileprovider";
            var uri = FileProvider.GetUriForFile(_activity, authority, documentFile);
            if (uri == null)
            {
                errorMessage = "Failed to create document URI.";
                return false;
            }

            var mimeType = string.IsNullOrWhiteSpace(contentType)
                ? MimeTypeMap.Singleton?.GetMimeTypeFromExtension(Path.GetExtension(fullPath).TrimStart('.')) ?? "application/octet-stream"
                : contentType;
            var intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, mimeType);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            _activity.StartActivity(intent);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
