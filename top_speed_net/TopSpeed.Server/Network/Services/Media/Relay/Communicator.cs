using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void OnCommunicatorMediaBegin(PlayerConnection player, PacketPlayerMediaBegin begin)
        {
            if (!_config.Features.VoiceChat)
                return;
            if (begin.PlayerId != player.Id || begin.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidCommunicatorMediaBegin(begin))
                return;

            var extension = (begin.FileExtension ?? string.Empty).Trim();
            if (extension.Length > ProtocolConstants.MaxMediaFileExtensionLength)
                extension = extension.Substring(0, ProtocolConstants.MaxMediaFileExtensionLength);

            player.IncomingCommunicatorMedia = new InMedia
            {
                MediaId = begin.MediaId,
                TransferId = begin.TransferId,
                State = MediaTransferState.Receiving,
                Extension = extension,
                TotalBytes = begin.TotalBytes,
                NextChunk = 0,
                BufferEnabled = true,
                Buffer = new byte[begin.TotalBytes],
                Offset = 0
            };

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerCommunicatorMediaBegin(new PacketPlayerMediaBegin
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    MediaId = begin.MediaId,
                    TransferId = begin.TransferId,
                    TotalBytes = begin.TotalBytes,
                    FileExtension = extension,
                    FrequencyTenths = begin.FrequencyTenths
                }),
                PacketStream.Media);
        }

        private void OnCommunicatorMediaChunk(PlayerConnection player, PacketPlayerMediaChunk chunk)
        {
            if (!_config.Features.VoiceChat)
                return;
            if (chunk.PlayerId != player.Id || chunk.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidMediaChunk(chunk))
                return;

            var transfer = player.IncomingCommunicatorMedia;
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
                player.IncomingCommunicatorMedia = null;
                return;
            }

            if (transfer.BufferEnabled)
            {
                if (transfer.Buffer == null || transfer.Buffer.Length < transfer.Offset + chunk.Data.Length)
                {
                    transfer.State = MediaTransferState.Cancelled;
                    player.IncomingCommunicatorMedia = null;
                    return;
                }

                Buffer.BlockCopy(chunk.Data, 0, transfer.Buffer, transfer.Offset, chunk.Data.Length);
            }

            transfer.Offset += chunk.Data.Length;
            transfer.NextChunk++;
            if (transfer.IsComplete)
                transfer.State = MediaTransferState.Complete;

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerCommunicatorMediaChunk(new PacketPlayerMediaChunk
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    MediaId = transfer.MediaId,
                    TransferId = transfer.TransferId,
                    ChunkIndex = chunk.ChunkIndex,
                    Data = chunk.Data
                }),
                PacketStream.Media);
        }

        private void OnCommunicatorMediaEnd(PlayerConnection player, PacketPlayerMediaEnd end)
        {
            if (!_config.Features.VoiceChat)
                return;
            if (end.PlayerId != player.Id || end.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidMediaEnd(end))
                return;

            var transfer = player.IncomingCommunicatorMedia;
            if (transfer == null)
                return;
            if (transfer.MediaId != end.MediaId || transfer.TransferId != end.TransferId || !transfer.IsComplete)
            {
                transfer.State = MediaTransferState.Cancelled;
                player.IncomingCommunicatorMedia = null;
                return;
            }

            if (transfer.BufferEnabled && transfer.Buffer != null && transfer.Buffer.Length == transfer.Offset)
            {
                player.CommunicatorMediaBlob = new MediaBlob
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
                player.CommunicatorMediaBlob = null;
            }

            player.IncomingCommunicatorMedia = null;

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerCommunicatorMediaEnd(new PacketPlayerMediaEnd
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    MediaId = transfer.MediaId,
                    TransferId = transfer.TransferId
                }),
                PacketStream.Media);
        }

        private void OnCommunicatorMediaState(PlayerConnection player, PacketPlayerCommunicatorMediaState state)
        {
            if (!_config.Features.VoiceChat)
                return;
            if (state.PlayerId != player.Id || state.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidCommunicatorMediaState(state))
                return;

            var mediaLoaded = state.MediaLoaded && state.MediaId != 0;
            var mediaPlaying = mediaLoaded && state.MediaPlaying;

            player.CommunicatorMedia = new CommunicatorMediaState
            {
                MediaId = mediaLoaded ? state.MediaId : 0u,
                FrequencyTenths = state.FrequencyTenths,
                MediaLoaded = mediaLoaded,
                MediaPlaying = mediaPlaying,
                VolumePercent = state.VolumePercent
            };

            if (!mediaLoaded)
                player.CommunicatorMediaBlob = null;

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerCommunicatorMediaState(new PacketPlayerCommunicatorMediaState
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    MediaId = mediaLoaded ? state.MediaId : 0u,
                    FrequencyTenths = state.FrequencyTenths,
                    MediaLoaded = mediaLoaded,
                    MediaPlaying = mediaPlaying,
                    VolumePercent = state.VolumePercent
                }),
                PacketStream.Media,
                PacketDeliveryKind.ReliableOrdered);
        }

        private void StopCommunicatorMedia(PlayerConnection player, bool notifyListeners)
        {
            if (player == null)
                return;

            var previous = player.CommunicatorMedia;
            player.IncomingCommunicatorMedia = null;
            player.CommunicatorMediaBlob = null;
            player.CommunicatorMedia = null;

            if (!notifyListeners)
                return;
            if (previous == null)
                return;
            if (!previous.MediaLoaded && !previous.MediaPlaying)
                return;

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerCommunicatorMediaState(new PacketPlayerCommunicatorMediaState
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    MediaId = 0,
                    FrequencyTenths = previous.FrequencyTenths,
                    MediaLoaded = false,
                    MediaPlaying = false,
                    VolumePercent = previous.VolumePercent
                }),
                PacketStream.Media,
                PacketDeliveryKind.ReliableOrdered);
        }

        private void SyncCommunicatorMediaTo(PlayerConnection receiver)
        {
            if (!_config.Features.VoiceChat)
                return;

            foreach (var owner in _players.Values)
            {
                if (owner.Id == receiver.Id)
                    continue;

                var state = owner.CommunicatorMedia;
                var blob = owner.CommunicatorMediaBlob;
                if (blob != null
                    && state != null
                    && state.MediaLoaded
                    && blob.MediaId == state.MediaId
                    && blob.Data != null
                    && blob.Data.Length > 0)
                {
                    _notify.ToPlayer(receiver, PacketSerializer.WritePlayerCommunicatorMediaBegin(new PacketPlayerMediaBegin
                    {
                        PlayerId = owner.Id,
                        PlayerNumber = owner.PlayerNumber,
                        MediaId = blob.MediaId,
                        TransferId = blob.TransferId,
                        TotalBytes = (uint)blob.Data.Length,
                        FileExtension = blob.Extension,
                        FrequencyTenths = state.FrequencyTenths
                    }), PacketStream.Media);

                    var chunkIndex = 0;
                    var offset = 0;
                    while (offset < blob.Data.Length)
                    {
                        var length = Math.Min(ProtocolConstants.MaxMediaChunkBytes, blob.Data.Length - offset);
                        var chunk = new byte[length];
                        Buffer.BlockCopy(blob.Data, offset, chunk, 0, length);
                        _notify.ToPlayer(receiver, PacketSerializer.WritePlayerCommunicatorMediaChunk(new PacketPlayerMediaChunk
                        {
                            PlayerId = owner.Id,
                            PlayerNumber = owner.PlayerNumber,
                            MediaId = blob.MediaId,
                            TransferId = blob.TransferId,
                            ChunkIndex = (ushort)chunkIndex,
                            Data = chunk
                        }), PacketStream.Media);
                        offset += length;
                        chunkIndex++;
                    }

                    _notify.ToPlayer(receiver, PacketSerializer.WritePlayerCommunicatorMediaEnd(new PacketPlayerMediaEnd
                    {
                        PlayerId = owner.Id,
                        PlayerNumber = owner.PlayerNumber,
                        MediaId = blob.MediaId,
                        TransferId = blob.TransferId
                    }), PacketStream.Media);
                }

                if (state == null)
                    continue;

                _notify.ToPlayer(receiver, PacketSerializer.WritePlayerCommunicatorMediaState(new PacketPlayerCommunicatorMediaState
                {
                    PlayerId = owner.Id,
                    PlayerNumber = owner.PlayerNumber,
                    MediaId = state.MediaLoaded ? state.MediaId : 0u,
                    FrequencyTenths = state.FrequencyTenths,
                    MediaLoaded = state.MediaLoaded,
                    MediaPlaying = state.MediaPlaying,
                    VolumePercent = state.VolumePercent
                }), PacketStream.Media, PacketDeliveryKind.ReliableOrdered);
            }
        }
    }
}
