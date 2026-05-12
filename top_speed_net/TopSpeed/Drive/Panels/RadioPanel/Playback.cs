using System;
using TopSpeed.Audio;
using TopSpeed.Localization;

namespace TopSpeed.Drive.Panels
{
    internal sealed partial class RadioVehiclePanel
    {
        private void TogglePlayback()
        {
            if (!_radio.HasMedia)
            {
                if (_playlist.Count == 0)
                {
                    _announce(LocalizationService.Translate(LocalizationService.Mark("No radio media loaded.")));
                    return;
                }

                SelectIndexFromCurrentMedia();
                if (_playlistIndex < 0)
                    _playlistIndex = 0;
                if (!LoadPlaylistEntry(_playlistIndex, preservePlaybackState: false, announceLoaded: true))
                    return;
                _radio.SetPlayback(true);
                _announce(LocalizationService.Translate(LocalizationService.Mark("playing")));
                _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
                return;
            }

            _radio.TogglePlayback();
            _announce(_radio.DesiredPlaying
                ? LocalizationService.Translate(LocalizationService.Mark("playing"))
                : LocalizationService.Translate(LocalizationService.Mark("paused")));
            _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
        }

        private void CycleTrack(int delta)
        {
            if (_playlist.Count == 0)
            {
                _announce(LocalizationService.Translate(LocalizationService.Mark("No folder playlist loaded.")));
                return;
            }

            SelectIndexFromCurrentMedia();
            if (!StepPlaylistIndex(delta))
                return;

            LoadPlaylistEntry(_playlistIndex, preservePlaybackState: true, announceLoaded: true);
        }

        private void ToggleShuffle()
        {
            _shuffleMode = !_shuffleMode;
            _settings.RadioShuffle = _shuffleMode;
            SaveRadioSettings();

            var lastFolder = _settings.RadioLastFolder ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(lastFolder))
                BuildPlaylistFromFolder(lastFolder, preserveCurrentMedia: true, announceErrors: false);

            _announce(_shuffleMode
                ? LocalizationService.Translate(LocalizationService.Mark("Shuffle mode on."))
                : LocalizationService.Translate(LocalizationService.Mark("Shuffle mode off.")));
        }

        private void ToggleLoop()
        {
            _loopMode = !_loopMode;
            ApplyLoopMode();
            _announce(_loopMode
                ? LocalizationService.Translate(LocalizationService.Mark("Loop mode on."))
                : LocalizationService.Translate(LocalizationService.Mark("Loop mode off.")));
        }

        private bool LoadPlaylistEntry(int index, bool preservePlaybackState, bool announceLoaded)
        {
            if (index < 0 || index >= _playlist.Count)
                return false;

            var mediaPath = _playlist[index];
            _playlistIndex = index;
            ApplyLoopMode();

            var mediaId = _nextMediaId();
            if (!_radio.TryLoadFromFile(mediaPath, mediaId, preservePlaybackState, out var error))
            {
                _announce(LocalizationService.Format(
                    LocalizationService.Mark("Failed to load radio media. {0}"),
                    error));
                return false;
            }

            if (announceLoaded)
            {
                var fileName = MediaPlaylist.GetDisplayName(mediaPath);
                _announce(fileName);
            }

            _mediaLoaded?.Invoke(mediaId, mediaPath);
            _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
            _lastObservedPlaying = _radio.HasMedia && _radio.IsPlaying;
            return true;
        }

        private void SelectIndexFromCurrentMedia()
        {
            if (_playlist.Count == 0)
            {
                _playlistIndex = -1;
                return;
            }

            var currentPath = _radio.MediaPath;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                var idx = _playlist.FindIndex(path => string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    _playlistIndex = idx;
                    return;
                }
            }

            if (_playlistIndex < 0 || _playlistIndex >= _playlist.Count)
                _playlistIndex = 0;
        }

        private bool StepPlaylistIndex(int delta)
        {
            if (_playlist.Count == 0)
                return false;

            if (_playlistIndex < 0 || _playlistIndex >= _playlist.Count)
                _playlistIndex = 0;
            else
                _playlistIndex += delta;

            while (_playlistIndex < 0)
                _playlistIndex += _playlist.Count;
            while (_playlistIndex >= _playlist.Count)
                _playlistIndex -= _playlist.Count;

            return true;
        }

        private void ApplyLoopMode()
        {
            _radio.SetLoopPlayback(_loopMode || _playlist.Count <= 1);
        }
    }
}


