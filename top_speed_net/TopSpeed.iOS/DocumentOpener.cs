using System;
using System.IO;
using Foundation;
using TopSpeed.Runtime;
using UIKit;

namespace TopSpeed.iOS;

internal sealed class IosDocumentOpener : IDocumentOpener
{
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
            var url = NSUrl.FromFilename(fullPath);
            if (url == null)
            {
                errorMessage = "Failed to resolve file URL.";
                return false;
            }

            var application = UIApplication.SharedApplication;
            if (!application.CanOpenUrl(url))
            {
                errorMessage = "iOS could not open this document type.";
                return false;
            }

            application.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
