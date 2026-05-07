using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RefreshServerPackages()
        {
            var tracksRoot = GetServerTracksDirectory();
            var discovered = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(tracksRoot))
            {
                RemoveStaleServerPackages(discovered);
                return;
            }

            var files = EnumerateServerTrackFiles(tracksRoot);
            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file))
                    continue;

                DateTime lastWriteUtc;
                try
                {
                    lastWriteUtc = File.GetLastWriteTimeUtc(file);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                discovered[file] = lastWriteUtc;
                if (HasServerPackageForSource(file, lastWriteUtc))
                    continue;

                if (!TryBuildServerTrackPackage(file, out var payload, out var bytes, out var error))
                {
                    _logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Skipping server track package '{0}': {1}"),
                        file,
                        error));
                    continue;
                }

                var hash = TrackPackageRef.NormalizeHash(payload.Manifest.Hash);
                if (_trackPackageCache.TryGetValue(hash, out var existing) && existing != null)
                {
                    existing.FromServerTracksFolder = true;
                    existing.SourcePath = file;
                    existing.SourceLastWriteUtc = lastWriteUtc;
                    continue;
                }

                StorePackage(
                    payload,
                    bytes,
                    fromServerTracksFolder: true,
                    sourcePath: file,
                    sourceLastWriteUtc: lastWriteUtc);
            }

            RemoveStaleServerPackages(discovered);
        }

        private void RemoveStaleServerPackages(IReadOnlyDictionary<string, DateTime> discovered)
        {
            var keys = _trackPackageCache
                .Where(pair => pair.Value != null && pair.Value.FromServerTracksFolder)
                .Where(pair =>
                {
                    var sourcePath = pair.Value.SourcePath ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(sourcePath))
                        return true;

                    if (!discovered.TryGetValue(sourcePath, out var sourceLastWriteUtc))
                        return true;

                    return pair.Value.SourceLastWriteUtc != sourceLastWriteUtc;
                })
                .Select(pair => pair.Key)
                .ToArray();

            for (var i = 0; i < keys.Length; i++)
            {
                if (IsPackageInUse(keys[i]))
                    continue;
                _trackPackageCache.Remove(keys[i]);
            }
        }

        private bool IsPackageInUse(string hash)
        {
            var normalizedHash = TrackPackageRef.NormalizeHash(hash);
            foreach (var room in _rooms.Values)
            {
                if (room.TrackSelection == null || !room.TrackSelection.IsCustomPackage)
                    continue;
                if (string.Equals(
                        TrackPackageRef.NormalizeHash(room.TrackSelection.Hash),
                        normalizedHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasServerPackageForSource(string sourcePath, DateTime sourceLastWriteUtc)
        {
            foreach (var package in _trackPackageCache.Values)
            {
                if (package == null || !package.FromServerTracksFolder)
                    continue;
                if (!string.Equals(package.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (package.SourceLastWriteUtc != sourceLastWriteUtc)
                    continue;
                return true;
            }

            return false;
        }

        private static string GetServerTracksDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, "Tracks");
        }

        private static IReadOnlyList<string> EnumerateServerTrackFiles(string tracksRoot)
        {
            if (!Directory.Exists(tracksRoot))
                return Array.Empty<string>();

            try
            {
                return Directory.EnumerateFiles(tracksRoot, "*.tsm", SearchOption.AllDirectories)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        private static bool TryBuildServerTrackPackage(
            string trackFile,
            out TrackPackagePayload payload,
            out byte[] bytes,
            out string error)
        {
            return PackageBuild.TryBuildPackageFromTrackFile(
                trackFile,
                string.Empty,
                fallbackLaps: 3,
                out payload,
                out bytes,
                out error);
        }
    }
}

