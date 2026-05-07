using System;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void CleanupLiveStreams()
        {
            var now = DateTime.UtcNow;
            foreach (var player in _players.Values)
            {
                var live = player.Live;
                if (live == null)
                    continue;
                if (now - live.LastFrameUtc <= LiveTimeout)
                    continue;

                if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                {
                    player.Live = null;
                    continue;
                }

                StopLive(player, room, notifyRoom: true);
            }
        }
    }
}
