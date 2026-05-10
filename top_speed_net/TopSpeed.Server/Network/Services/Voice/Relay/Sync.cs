using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void StopVoice(PlayerConnection player, GameRoom room, bool notifyRoom)
        {
            var voice = player.Voice;
            if (voice == null)
                return;

            player.Voice = null;
            if (!notifyRoom)
                return;

            _notify.ToRoomExcept(
                room,
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

        private void SyncVoiceTo(GameRoom room, PlayerConnection receiver)
        {
            if (!_config.Features.VoiceChat)
                return;

            foreach (var id in room.PlayerIds)
            {
                if (id == receiver.Id)
                    continue;
                if (!_players.TryGetValue(id, out var owner))
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
