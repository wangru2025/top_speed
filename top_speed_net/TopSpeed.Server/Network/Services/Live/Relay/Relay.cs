using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static readonly TimeSpan LiveTimeout = TimeSpan.FromMilliseconds(ProtocolConstants.LiveTimeoutMs);

        private void OnLiveStart(PlayerConnection player, PacketPlayerLiveStart start)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                return;
            if (start.PlayerId != player.Id || start.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidLiveStart(start))
                return;

            if (player.Live != null)
                StopLive(player, room, notifyRoom: true);

            player.Live = new LiveState
            {
                StreamId = start.StreamId,
                Codec = start.Codec,
                SampleRate = start.SampleRate,
                Channels = start.Channels,
                FrameMs = start.FrameMs,
                NextSequence = 0,
                HasSequence = false,
                LastFrameUtc = DateTime.UtcNow
            };

            _notify.ToRoomExcept(
                room,
                player.Id,
                PacketSerializer.WritePlayerLiveStart(new PacketPlayerLiveStart
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    StreamId = start.StreamId,
                    Codec = start.Codec,
                    SampleRate = start.SampleRate,
                    Channels = start.Channels,
                    FrameMs = start.FrameMs
                }),
                PacketStream.Live,
                PacketDeliveryKind.ReliableOrdered);
        }

        private void OnLiveFrame(PlayerConnection player, PacketPlayerLiveFrame frame)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                return;
            if (frame.PlayerId != player.Id || frame.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidLiveFrame(frame))
                return;

            var live = player.Live;
            if (live == null)
                return;
            if (live.StreamId != frame.StreamId)
                return;

            if (live.HasSequence && frame.Sequence != live.NextSequence)
            {
                if (!IsNewerSequence(frame.Sequence, live.NextSequence))
                    return;
            }

            live.HasSequence = true;
            live.NextSequence = unchecked((ushort)(frame.Sequence + 1));
            live.LastFrameUtc = DateTime.UtcNow;

            _notify.ToRoomExcept(
                room,
                player.Id,
                PacketSerializer.WritePlayerLiveFrame(new PacketPlayerLiveFrame
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    StreamId = live.StreamId,
                    Sequence = frame.Sequence,
                    Timestamp = frame.Timestamp,
                    Data = frame.Data
                }),
                PacketStream.Live);
        }

        private void OnLiveStop(PlayerConnection player, PacketPlayerLiveStop stop)
        {
            if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                return;
            if (stop.PlayerId != player.Id || stop.PlayerNumber != player.PlayerNumber)
                return;
            if (!PacketValidation.IsValidLiveStop(stop))
                return;

            var live = player.Live;
            if (live == null)
                return;
            if (live.StreamId != stop.StreamId)
                return;

            StopLive(player, room, notifyRoom: true);
        }

        private static bool IsNewerSequence(ushort sequence, ushort expected)
        {
            var delta = (ushort)(sequence - expected);
            return delta != 0 && delta < 32768;
        }
    }
}
