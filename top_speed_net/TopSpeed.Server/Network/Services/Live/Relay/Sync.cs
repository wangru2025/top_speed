using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void StopLive(PlayerConnection player, GameRoom room, bool notifyRoom)
        {
            var live = player.Live;
            if (live == null)
                return;

            player.Live = null;
            if (!notifyRoom)
                return;

            _notify.ToRoomExcept(
                room,
                player.Id,
                PacketSerializer.WritePlayerLiveStop(new PacketPlayerLiveStop
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    StreamId = live.StreamId
                }),
                PacketStream.Live,
                PacketDeliveryKind.ReliableOrdered);
        }

        private void SyncLiveTo(GameRoom room, PlayerConnection receiver)
        {
            foreach (var id in room.PlayerIds)
            {
                if (id == receiver.Id)
                    continue;
                if (!_players.TryGetValue(id, out var owner))
                    continue;

                var live = owner.Live;
                if (live == null || live.StreamId == 0)
                    continue;

                _notify.ToPlayer(
                    receiver,
                    PacketSerializer.WritePlayerLiveStart(new PacketPlayerLiveStart
                    {
                        PlayerId = owner.Id,
                        PlayerNumber = owner.PlayerNumber,
                        StreamId = live.StreamId,
                        Codec = live.Codec,
                        SampleRate = live.SampleRate,
                        Channels = live.Channels,
                        FrameMs = live.FrameMs
                    }),
                    PacketStream.Live,
                    PacketDeliveryKind.ReliableOrdered);
            }
        }
    }
}

