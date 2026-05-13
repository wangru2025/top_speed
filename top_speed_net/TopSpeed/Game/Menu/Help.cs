using System;
using System.Diagnostics;
using TopSpeed.Core;
using TopSpeed.Localization;
using TopSpeed.Runtime;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private const string GameGuideFileName = "game-guide.html";
        private const string TrackGuideFileName = "track-creation-guide.html";
        private const string VehicleGuideFileName = "vehicle-physics-and-creation-guide.html";
        private const string ChangeLogFileName = "changes.html";

        private void OpenGameGuide()
        {
            OpenHelpDocument(GameGuideFileName);
        }

        private void OpenTrackCreationGuide()
        {
            OpenHelpDocument(TrackGuideFileName);
        }

        private void OpenVehicleCreationGuide()
        {
            OpenHelpDocument(VehicleGuideFileName);
        }

        private void OpenChangeLogFile()
        {
            OpenHelpDocument(ChangeLogFileName);
        }

        private void OpenHelpDocument(string fileName)
        {
            string? path = null;
            foreach (var candidate in HelpDocumentResolver.BuildCandidateRelativePaths(fileName, _settings.Language))
            {
                path = AssetPaths.ResolveExistingPath("docs", candidate);
                if (path != null)
                    break;
            }

            if (path == null)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Help file not found"),
                    LocalizationService.Mark("The requested help file could not be found."),
                    new[] { fileName });
                return;
            }

            try
            {
                if (DocumentOpenRuntime.TryOpenDocument(path, "text/html", out var platformError))
                    return;

                if (OperatingSystem.IsAndroid())
                    throw new InvalidOperationException(platformError);

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    LocalizationService.Mark("Unable to open help file"),
                    LocalizationService.Mark("The requested help file could not be opened."),
                    new[] { ex.Message });
            }
        }
    }
}
