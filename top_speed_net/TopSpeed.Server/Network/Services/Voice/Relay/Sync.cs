using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void StopVoice(PlayerConnection player, bool notifyRoom)
        {
            var voice = player.Voice;
            if (voice == null)
                return;

            player.Voice = null;
            if (!notifyRoom)
                return;

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerVoiceStop(new PacketPlayerVoiceStop
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    StreamId = voice.StreamId
                }),
                PacketStream.Voice,
                PacketDeliveryKind.ReliableOrdered);
        }

        // Sync every currently-active voice stream to a freshly-connected (or
        // resuming) receiver, so they hear ongoing speakers without having to
        // wait for the next VoiceStart. Voice is relayed server-wide, so this
        // walks every connected player rather than just one room.
        private void SyncVoiceTo(PlayerConnection receiver)
        {
            if (!_config.Features.VoiceChat)
                return;

            foreach (var owner in _players.Values)
            {
                if (owner.Id == receiver.Id)
                    continue;

                var voice = owner.Voice;
                if (voice == null || voice.StreamId == 0)
                    continue;

                _notify.ToPlayer(
                    receiver,
                    PacketSerializer.WritePlayerVoiceStart(new PacketPlayerVoiceStart
                    {
                        PlayerId = owner.Id,
                        PlayerNumber = owner.PlayerNumber,
                        StreamId = voice.StreamId,
                        Codec = voice.Codec,
                        SampleRate = voice.SampleRate,
                        Channels = voice.Channels,
                        FrameMs = voice.FrameMs,
                        FrequencyTenths = voice.FrequencyTenths,
                        PushToTalk = voice.PushToTalk
                    }),
                    PacketStream.Voice,
                    PacketDeliveryKind.ReliableOrdered);
            }
        }
    }
}
