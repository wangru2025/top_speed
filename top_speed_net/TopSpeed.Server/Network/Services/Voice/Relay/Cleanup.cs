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

                StopVoice(player, notifyRoom: true);
            }
        }
    }
}
