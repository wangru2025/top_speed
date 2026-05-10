using System;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void CleanupVoiceStreams()
        {
            var now = DateTime.UtcNow;
            foreach (var player in _players.Values)
            {
                var voice = player.Voice;
                if (voice == null)
                    continue;
                if (now - voice.LastFrameUtc <= VoiceTimeout)
                    continue;

                if (!player.RoomId.HasValue || !_rooms.TryGetValue(player.RoomId.Value, out var room))
                {
                    player.Voice = null;
                    continue;
                }

                StopVoice(player, room, notifyRoom: true);
            }
        }
    }
}
