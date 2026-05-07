using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private bool TryGetPackage(string hash, out PackageRecord package)
        {
            package = null!;
            var key = TrackPackageRef.NormalizeHash(hash);
            if (string.IsNullOrWhiteSpace(key))
                return false;
            if (!_trackPackageCache.TryGetValue(key, out var found) || found == null)
                return false;

            package = found;
            package.LastAccessUtc = DateTime.UtcNow;
            return true;
        }

        private bool StorePackage(
            TrackPackagePayload payload,
            byte[] bytes,
            bool fromServerTracksFolder = false,
            string sourcePath = "",
            DateTime? sourceLastWriteUtc = null)
        {
            if (payload == null || bytes == null)
                return false;

            var hash = TrackPackageRef.NormalizeHash(payload.Manifest.Hash);
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            var trackData = TrackPackageCodec.ToTrackData(payload, userDefined: true, sourcePath: string.Empty);
            _trackPackageCache[hash] = new PackageRecord
            {
                Ref = TrackPackageRef.Custom(payload.Manifest.TrackId, payload.Manifest.Version, hash),
                Payload = payload,
                Bytes = bytes,
                TrackData = trackData,
                LastAccessUtc = DateTime.UtcNow,
                FromServerTracksFolder = fromServerTracksFolder,
                SourcePath = sourcePath ?? string.Empty,
                SourceLastWriteUtc = sourceLastWriteUtc ?? DateTime.MinValue
            };

            EvictTrackPackages();
            return true;
        }

        private void EvictTrackPackages()
        {
            if (_trackPackageCache.Count <= ProtocolConstants.MaxTrackPackageCacheEntries)
                return;

            var protectedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var room in _rooms.Values)
            {
                if (room.TrackSelection != null
                    && room.TrackSelection.IsCustomPackage
                    && !string.IsNullOrWhiteSpace(room.TrackSelection.Hash))
                {
                    protectedHashes.Add(TrackPackageRef.NormalizeHash(room.TrackSelection.Hash));
                }
            }

            var candidates = _trackPackageCache
                .Where(pair => !protectedHashes.Contains(pair.Key))
                .OrderBy(pair => pair.Value.LastAccessUtc)
                .Select(pair => pair.Key)
                .ToList();

            for (var i = 0; i < candidates.Count && _trackPackageCache.Count > ProtocolConstants.MaxTrackPackageCacheEntries; i++)
                _trackPackageCache.Remove(candidates[i]);
        }
    }
}

