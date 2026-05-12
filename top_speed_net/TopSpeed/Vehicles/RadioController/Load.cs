using System;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed partial class VehicleRadioController
    {
        public bool TryLoadFromFile(string path, uint mediaId, bool preservePlaybackState, out string error)
        {
            error = string.Empty;
            string? tempPlaybackPath = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = LocalizationService.Mark("No media file selected.");
                return false;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    error = LocalizationService.Mark("The selected media file does not exist.");
                    return false;
                }

                var playablePath = PreparePlayablePath(fullPath, mediaId, _options.TempFolderName, out tempPlaybackPath);
                var wasPlaying = preservePlaybackState ? _desiredPlaying : false;
                DisposeSource();
                var asset = _audio.LoadStream(playablePath);
                _source = _options.Spatialize
                    ? _audio.CreateSpatialSource(asset, _options.BusName, allowHrtf: _options.AllowHrtf)
                    : _audio.CreateSource(asset, _options.BusName, useHrtf: _options.AllowHrtf);
                _source.SetDopplerFactor(0f);
                ApplySourceVolume();
                _mediaPath = fullPath;
                _mediaId = mediaId;
                _desiredPlaying = wasPlaying;
                ReplaceOwnedTempFile(tempPlaybackPath);
                if (_desiredPlaying && !_pausedByGame)
                    _source.Play(loop: _loopPlayback);
                return true;
            }
            catch (IOException ex)
            {
                SafeDelete(tempPlaybackPath);
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                SafeDelete(tempPlaybackPath);
                error = ex.Message;
                return false;
            }
            catch (ArgumentException ex)
            {
                SafeDelete(tempPlaybackPath);
                error = ex.Message;
                return false;
            }
            catch (NotSupportedException ex)
            {
                SafeDelete(tempPlaybackPath);
                error = ex.Message;
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                SafeDelete(tempPlaybackPath);
                error = ex.Message;
                return false;
            }
            catch (InvalidOperationException ex)
            {
                SafeDelete(tempPlaybackPath);
                error = ex.Message;
                return false;
            }
        }

        public bool TryLoadFromBytes(byte[] data, string extension, uint mediaId, bool preservePlaybackState, out string error)
        {
            error = string.Empty;
            if (data == null || data.Length == 0)
            {
                error = LocalizationService.Mark("Received media is empty.");
                return false;
            }

            try
            {
                var folder = Path.Combine(Path.GetTempPath(), "TopSpeed", _options.TempFolderName);
                Directory.CreateDirectory(folder);
                var normalizedExtension = NormalizeExtension(extension);
                var path = Path.Combine(folder, $"radio_{mediaId}_{Guid.NewGuid():N}{normalizedExtension}");
                File.WriteAllBytes(path, data);
                if (TryLoadFromFile(path, mediaId, preservePlaybackState, out error))
                {
                    ReplaceOwnedTempFile(path);
                    return true;
                }

                SafeDelete(path);
                return false;
            }
            catch (IOException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (NotSupportedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (InvalidOperationException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string NormalizeExtension(string extension)
        {
            var trimmed = (extension ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return ".bin";
            if (!trimmed.StartsWith(".", StringComparison.Ordinal))
                trimmed = "." + trimmed;
            if (trimmed.Length > ProtocolConstants.MaxMediaFileExtensionLength + 1)
                trimmed = trimmed.Substring(0, ProtocolConstants.MaxMediaFileExtensionLength + 1);
            for (var i = 1; i < trimmed.Length; i++)
            {
                var ch = trimmed[i];
                if (!char.IsLetterOrDigit(ch))
                    return ".bin";
            }

            return trimmed;
        }

        private static string PreparePlayablePath(string path, uint mediaId, out string? tempPlaybackPath)
        {
            return PreparePlayablePath(path, mediaId, "Radio", out tempPlaybackPath);
        }

        private static string PreparePlayablePath(string path, uint mediaId, string tempFolderName, out string? tempPlaybackPath)
        {
            tempPlaybackPath = null;
            if (!RequiresAsciiPlaybackPath(path))
                return path;

            var folder = Path.Combine(Path.GetTempPath(), "TopSpeed", string.IsNullOrWhiteSpace(tempFolderName) ? "Radio" : tempFolderName);
            Directory.CreateDirectory(folder);
            var extension = NormalizeExtension(Path.GetExtension(path));
            tempPlaybackPath = Path.Combine(folder, $"radio_local_{mediaId}_{Guid.NewGuid():N}{extension}");
            File.Copy(path, tempPlaybackPath, overwrite: true);
            return tempPlaybackPath;
        }

        private static bool RequiresAsciiPlaybackPath(string path)
        {
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] > 127)
                    return true;
            }

            return false;
        }
    }
}

