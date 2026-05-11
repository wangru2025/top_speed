using System;
using TopSpeed.Audio;
using TopSpeed.Localization;

namespace TopSpeed.Drive.Panels
{
    internal sealed partial class RadioVehiclePanel
    {
        private bool BuildPlaylistFromFolder(string folderPath, bool preserveCurrentMedia, bool announceErrors)
        {
            if (!MediaPlaylist.TryBuildFromFolder(
                    folderPath,
                    _shuffleMode,
                    _random,
                    out var fullFolder,
                    out var files,
                    out var error))
            {
                if (announceErrors)
                    AnnounceFolderError(error);
                return false;
            }

            var currentPath = preserveCurrentMedia ? _radio.MediaPath : null;

            _playlist.Clear();
            _playlist.AddRange(files);
            _playlistFolder = fullFolder;
            _playlistIndex = 0;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var idx = _playlist.FindIndex(path => string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                    _playlistIndex = idx;
            }

            _settings.RadioLastFolder = _playlistFolder;
            _settings.RadioShuffle = _shuffleMode;
            SaveRadioSettings();
            ApplyLoopMode();
            return true;
        }

        private void TryRestoreFolderPlaylist()
        {
            if (string.IsNullOrWhiteSpace(_settings.RadioLastFolder))
                return;

            BuildPlaylistFromFolder(_settings.RadioLastFolder, preserveCurrentMedia: false, announceErrors: false);
        }

        private void AnnounceFolderError(MediaPlaylistFolderLoadError error)
        {
            switch (error)
            {
                case MediaPlaylistFolderLoadError.EmptyPath:
                    _announce(LocalizationService.Translate(LocalizationService.Mark("No folder was selected.")));
                    return;
                case MediaPlaylistFolderLoadError.InvalidPath:
                    _announce(LocalizationService.Translate(LocalizationService.Mark("The selected folder path is invalid.")));
                    return;
                case MediaPlaylistFolderLoadError.NotFound:
                    _announce(LocalizationService.Translate(LocalizationService.Mark("The selected folder does not exist.")));
                    return;
                case MediaPlaylistFolderLoadError.ReadFailed:
                    _announce(LocalizationService.Translate(LocalizationService.Mark("Could not read files from the selected folder.")));
                    return;
                case MediaPlaylistFolderLoadError.NoSupportedFiles:
                    _announce(LocalizationService.Translate(LocalizationService.Mark("No supported audio files were found in the selected folder.")));
                    return;
            }
        }
    }
}


