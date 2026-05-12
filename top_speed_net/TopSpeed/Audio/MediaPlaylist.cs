using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TopSpeed.Audio
{
    internal enum MediaPlaylistFolderLoadError
    {
        None = 0,
        EmptyPath = 1,
        InvalidPath = 2,
        NotFound = 3,
        ReadFailed = 4,
        NoSupportedFiles = 5
    }

    internal static class MediaPlaylist
    {
        private static readonly string[] SupportedExtensions = { ".wav", ".ogg", ".mp3", ".flac", ".aac", ".m4a" };

        public static bool IsSupportedAudioFile(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            for (var i = 0; i < SupportedExtensions.Length; i++)
            {
                if (string.Equals(extension, SupportedExtensions[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool TryBuildFromFolder(
            string folderPath,
            bool shuffle,
            Random? random,
            out string fullFolder,
            out List<string> files,
            out MediaPlaylistFolderLoadError error)
        {
            fullFolder = string.Empty;
            files = new List<string>();
            error = MediaPlaylistFolderLoadError.None;

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                error = MediaPlaylistFolderLoadError.EmptyPath;
                return false;
            }

            try
            {
                fullFolder = Path.GetFullPath(folderPath);
            }
            catch
            {
                error = MediaPlaylistFolderLoadError.InvalidPath;
                return false;
            }

            if (!Directory.Exists(fullFolder))
            {
                error = MediaPlaylistFolderLoadError.NotFound;
                return false;
            }

            try
            {
                files = Directory
                    .EnumerateFiles(fullFolder, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(IsSupportedAudioFile)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                error = MediaPlaylistFolderLoadError.ReadFailed;
                return false;
            }

            if (files.Count == 0)
            {
                error = MediaPlaylistFolderLoadError.NoSupportedFiles;
                return false;
            }

            if (shuffle)
                ShuffleInPlace(files, random ?? Random.Shared);

            return true;
        }

        public static void ShuffleInPlace(List<string> files, Random random)
        {
            if (files == null || files.Count < 2)
                return;

            for (var i = files.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var tmp = files[i];
                files[i] = files[j];
                files[j] = tmp;
            }
        }

        public static string GetDisplayName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            var name = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(name))
                return name;

            name = Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(name) ? path : name;
        }
    }
}
