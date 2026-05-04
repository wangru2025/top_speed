using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void HandleUploadBegin(PlayerConnection player, PacketTrackPackageUploadBegin packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (!_owner._config.Features.CustomTracks)
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, packet.Hash, LocalizationService.Mark("Custom tracks are disabled on this server."));
                    return;
                }
                if (!IsSelectionEnabled(room))
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, packet.Hash, LocalizationService.Mark("Custom tracks are not enabled for this room."));
                    return;
                }
                if (!PacketValidation.IsValidTrackPackageUploadBegin(packet))
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, packet.Hash, LocalizationService.Mark("Invalid track package upload request."));
                    return;
                }

                var hash = TrackPackageRef.NormalizeHash(packet.Hash);
                if (_owner.TryGetTrackPackage(hash, out _))
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Reused, hash, LocalizationService.Mark("Track package already exists on server."));
                    _owner.SendTrackPackageCatalogToRoom(room, _owner.BuildTrackPackageCatalog());
                    return;
                }

                _owner._trackPackageUploads[player.Id] = new PackageUploadSession
                {
                    UploadId = packet.UploadId,
                    OwnerPlayerId = player.Id,
                    RoomId = room.Id,
                    TrackId = packet.TrackId,
                    Version = packet.Version,
                    Hash = hash,
                    TotalBytes = packet.TotalBytes,
                    NextChunkIndex = 0,
                    Offset = 0,
                    Bytes = new byte[packet.TotalBytes]
                };
            }

            public void HandleUploadChunk(PlayerConnection player, PacketTrackPackageUploadChunk packet)
            {
                if (!PacketValidation.IsValidTrackPackageUploadChunk(packet))
                    return;
                if (!_owner._trackPackageUploads.TryGetValue(player.Id, out var session))
                    return;
                if (session.UploadId != packet.UploadId)
                    return;
                if (session.NextChunkIndex != packet.ChunkIndex)
                    return;
                if (!player.RoomId.HasValue
                    || player.RoomId.Value != session.RoomId
                    || !_owner._rooms.TryGetValue(session.RoomId, out var room)
                    || room.HostId != player.Id)
                {
                    _owner._trackPackageUploads.Remove(player.Id);
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, session.Hash, LocalizationService.Mark("Track package upload ownership changed."));
                    return;
                }

                var bytes = packet.Data ?? System.Array.Empty<byte>();
                if (session.Offset + bytes.Length > session.Bytes.Length)
                {
                    _owner._trackPackageUploads.Remove(player.Id);
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, session.Hash, LocalizationService.Mark("Track package upload exceeded declared size."));
                    return;
                }

                System.Buffer.BlockCopy(bytes, 0, session.Bytes, session.Offset, bytes.Length);
                session.Offset += bytes.Length;
                session.NextChunkIndex++;
            }

            public void HandleUploadEnd(PlayerConnection player, PacketTrackPackageUploadEnd packet)
            {
                if (!PacketValidation.IsValidTrackPackageUploadEnd(packet))
                    return;
                if (!_owner._trackPackageUploads.TryGetValue(player.Id, out var session))
                    return;
                if (session.UploadId != packet.UploadId)
                    return;
                if (!player.RoomId.HasValue
                    || player.RoomId.Value != session.RoomId
                    || !_owner._rooms.TryGetValue(session.RoomId, out var room)
                    || room.HostId != player.Id)
                {
                    _owner._trackPackageUploads.Remove(player.Id);
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, session.Hash, LocalizationService.Mark("Track package upload ownership changed."));
                    return;
                }

                _owner._trackPackageUploads.Remove(player.Id);
                if (session.Offset != session.Bytes.Length)
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, session.Hash, LocalizationService.Mark("Track package upload is incomplete."));
                    return;
                }

                if (!TrackPackageCodec.TryDeserialize(session.Bytes, out var payload, out var parseError))
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, session.Hash, parseError);
                    return;
                }

                var computedHash = TrackPackageCodec.ComputeHash(payload);
                if (!string.Equals(computedHash, session.Hash, System.StringComparison.OrdinalIgnoreCase))
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, session.Hash, LocalizationService.Mark("Track package hash mismatch."));
                    return;
                }

                payload.Manifest.Hash = computedHash;
                payload.Manifest.TrackId = session.TrackId;
                payload.Manifest.Version = session.Version;

                if (!_owner.StoreTrackPackage(payload, session.Bytes))
                {
                    _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Rejected, session.Hash, LocalizationService.Mark("Unable to store track package on server."));
                    return;
                }

                _owner.SendTrackPackageUploadResult(player, packet.UploadId, TrackPackageUploadStatus.Accepted, computedHash, LocalizationService.Mark("Track package uploaded successfully."));
                _owner.SendTrackPackageCatalogToRoom(room, _owner.BuildTrackPackageCatalog());
            }
        }
    }
}


