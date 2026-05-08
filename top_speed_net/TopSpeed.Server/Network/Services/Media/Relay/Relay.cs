using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void UpdateMediaState(PlayerConnection player, GameRoom room, PacketRacePlayerData data)
        {
            player.MediaLoaded = data.MediaLoaded && data.MediaId != 0;
            player.MediaPlaying = player.MediaLoaded && data.MediaPlaying;
            player.MediaId = player.MediaLoaded ? data.MediaId : 0u;
            if (!player.MediaLoaded)
            {
                room.MediaMap.Remove(player.Id);
            }
        }

        private void ResetMediaState(PlayerConnection player, GameRoom room)
        {
            player.IncomingMedia = null;
            player.MediaLoaded = false;
            player.MediaPlaying = false;
            player.MediaId = 0;
            room.MediaMap.Remove(player.Id);
        }

        private void OnMediaBegin(PlayerConnection player, PacketPlayerMediaBegin begin)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                return;
            if (begin.PlayerId != player.Id || begin.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidMediaBegin(begin))
                return;

            var extension = (begin.FileExtension ?? string.Empty).Trim();
            if (extension.Length > ProtocolConstants.MaxMediaFileExtensionLength)
                extension = extension.Substring(0, ProtocolConstants.MaxMediaFileExtensionLength);

            player.IncomingMedia = new InMedia
            {
                MediaId = begin.MediaId,
                TransferId = begin.TransferId,
                State = MediaTransferState.Receiving,
                Extension = extension,
                TotalBytes = begin.TotalBytes,
                NextChunk = 0,
                // Radio media is relayed chunk-by-chunk and is not buffered in full on the server.
                BufferEnabled = false,
                Buffer = Array.Empty<byte>(),
                Offset = 0
            };

            _notify.ToRoomExcept(room, player.Id, PacketSerializer.WritePlayerMediaBegin(new PacketPlayerMediaBegin
            {
                PlayerId = player.Id,
                PlayerNumber = player.PlayerNumber,
                MediaId = begin.MediaId,
                TransferId = begin.TransferId,
                TotalBytes = begin.TotalBytes,
                FileExtension = extension
            }), PacketStream.Media);
        }

        private void OnMediaChunk(PlayerConnection player, PacketPlayerMediaChunk chunk)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                return;
            if (chunk.PlayerId != player.Id || chunk.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidMediaChunk(chunk))
                return;
            var transfer = player.IncomingMedia;
            if (transfer == null)
                return;
            if (transfer.MediaId != chunk.MediaId || transfer.TransferId != chunk.TransferId)
                return;
            if (transfer.NextChunk != chunk.ChunkIndex)
                return;
            if (chunk.Data == null || chunk.Data.Length == 0 || chunk.Data.Length > ProtocolConstants.MaxMediaChunkBytes)
                return;

            var remaining = (int)transfer.TotalBytes - transfer.Offset;
            if (chunk.Data.Length > remaining)
            {
                transfer.State = MediaTransferState.Cancelled;
                player.IncomingMedia = null;
                return;
            }

            if (transfer.BufferEnabled)
            {
                if (transfer.Buffer == null || transfer.Buffer.Length < transfer.Offset + chunk.Data.Length)
                {
                    transfer.State = MediaTransferState.Cancelled;
                    player.IncomingMedia = null;
                    return;
                }

                Buffer.BlockCopy(chunk.Data, 0, transfer.Buffer, transfer.Offset, chunk.Data.Length);
            }
            transfer.Offset += chunk.Data.Length;
            transfer.NextChunk++;
            if (transfer.IsComplete)
                transfer.State = MediaTransferState.Complete;

            _notify.ToRoomExcept(room, player.Id, PacketSerializer.WritePlayerMediaChunk(new PacketPlayerMediaChunk
            {
                PlayerId = player.Id,
                PlayerNumber = player.PlayerNumber,
                MediaId = transfer.MediaId,
                TransferId = transfer.TransferId,
                ChunkIndex = chunk.ChunkIndex,
                Data = chunk.Data
            }), PacketStream.Media);
        }

        private void OnMediaEnd(PlayerConnection player, PacketPlayerMediaEnd end)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                return;
            if (end.PlayerId != player.Id || end.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidMediaEnd(end))
                return;
            var transfer = player.IncomingMedia;
            if (transfer == null)
                return;
            if (transfer.MediaId != end.MediaId || transfer.TransferId != end.TransferId || !transfer.IsComplete)
            {
                transfer.State = MediaTransferState.Cancelled;
                player.IncomingMedia = null;
                return;
            }

            if (transfer.BufferEnabled && transfer.Buffer != null && transfer.Buffer.Length == transfer.Offset)
            {
                room.MediaMap[player.Id] = new MediaBlob
                {
                    MediaId = transfer.MediaId,
                    TransferId = transfer.TransferId,
                    State = MediaTransferState.Complete,
                    Extension = transfer.Extension,
                    Data = transfer.Buffer
                };
            }
            else
            {
                room.MediaMap.Remove(player.Id);
            }
            player.IncomingMedia = null;

            _notify.ToRoomExcept(room, player.Id, PacketSerializer.WritePlayerMediaEnd(new PacketPlayerMediaEnd
            {
                PlayerId = player.Id,
                PlayerNumber = player.PlayerNumber,
                MediaId = transfer.MediaId,
                TransferId = transfer.TransferId
            }), PacketStream.Media);
        }

        private void SyncMediaTo(GameRoom room, PlayerConnection receiver)
        {
            foreach (var id in room.PlayerIds)
            {
                if (id == receiver.Id)
                    continue;
                if (!room.MediaMap.TryGetValue(id, out var media))
                    continue;
                if (!_players.TryGetValue(id, out var owner))
                    continue;
                if (media.MediaId == 0 || media.Data == null || media.Data.Length == 0)
                    continue;

                _notify.ToPlayer(receiver, PacketSerializer.WritePlayerMediaBegin(new PacketPlayerMediaBegin
                {
                    PlayerId = owner.Id,
                    PlayerNumber = owner.PlayerNumber,
                    MediaId = media.MediaId,
                    TransferId = media.TransferId,
                    TotalBytes = (uint)media.Data.Length,
                    FileExtension = media.Extension
                }), PacketStream.Media);

                var chunkIndex = 0;
                var offset = 0;
                while (offset < media.Data.Length)
                {
                    var length = Math.Min(ProtocolConstants.MaxMediaChunkBytes, media.Data.Length - offset);
                    var chunk = new byte[length];
                    Buffer.BlockCopy(media.Data, offset, chunk, 0, length);
                    _notify.ToPlayer(receiver, PacketSerializer.WritePlayerMediaChunk(new PacketPlayerMediaChunk
                    {
                        PlayerId = owner.Id,
                        PlayerNumber = owner.PlayerNumber,
                        MediaId = media.MediaId,
                        TransferId = media.TransferId,
                        ChunkIndex = (ushort)chunkIndex,
                        Data = chunk
                    }), PacketStream.Media);
                    offset += length;
                    chunkIndex++;
                }

                _notify.ToPlayer(receiver, PacketSerializer.WritePlayerMediaEnd(new PacketPlayerMediaEnd
                {
                    PlayerId = owner.Id,
                    PlayerNumber = owner.PlayerNumber,
                    MediaId = media.MediaId,
                    TransferId = media.TransferId
                }), PacketStream.Media);
            }
        }
    }
}

