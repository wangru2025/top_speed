using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static readonly TimeSpan VoiceTimeout = TimeSpan.FromMilliseconds(ProtocolConstants.VoiceTimeoutMs);

        // Voice chat is relayed server-wide. Frequency-tuning happens on the
        // receiver, so a transmitter does not need to be in the same room (or
        // any room) as the listener — anyone tuned to the same frequency on
        // the server should hear them, including in the lobby.
        private void OnVoiceStart(PlayerConnection player, PacketPlayerVoiceStart start)
        {
            if (!_config.Features.VoiceChat)
                return;
            if (start.PlayerId != player.Id)
                return;
            if (!PacketValidation.IsValidVoiceStart(start))
                return;

            if (player.Voice != null)
                StopVoice(player, notifyRoom: true);

            player.Voice = new VoiceState
            {
                StreamId = start.StreamId,
                Codec = start.Codec,
                SampleRate = start.SampleRate,
                Channels = start.Channels,
                FrameMs = start.FrameMs,
                FrequencyTenths = start.FrequencyTenths,
                PushToTalk = start.PushToTalk,
                NextSequence = 0,
                HasSequence = false,
                LastFrameUtc = DateTime.UtcNow
            };

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerVoiceStart(new PacketPlayerVoiceStart
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    StreamId = start.StreamId,
                    Codec = start.Codec,
                    SampleRate = start.SampleRate,
                    Channels = start.Channels,
                    FrameMs = start.FrameMs,
                    FrequencyTenths = start.FrequencyTenths,
                    PushToTalk = start.PushToTalk
                }),
                PacketStream.Voice,
                PacketDeliveryKind.ReliableOrdered);
        }

        private void OnVoiceFrame(PlayerConnection player, PacketPlayerVoiceFrame frame)
        {
            if (!_config.Features.VoiceChat)
                return;
            if (frame.PlayerId != player.Id)
                return;
            if (!PacketValidation.IsValidVoiceFrame(frame))
                return;

            var voice = player.Voice;
            if (voice == null)
                return;
            if (voice.StreamId != frame.StreamId)
                return;

            if (voice.HasSequence && frame.Sequence != voice.NextSequence)
            {
                if (!IsNewerSequence(frame.Sequence, voice.NextSequence))
                    return;
            }

            voice.HasSequence = true;
            voice.NextSequence = unchecked((ushort)(frame.Sequence + 1));
            voice.LastFrameUtc = DateTime.UtcNow;

            _notify.ToAllExcept(
                player.Id,
                PacketSerializer.WritePlayerVoiceFrame(new PacketPlayerVoiceFrame
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    StreamId = voice.StreamId,
                    Sequence = frame.Sequence,
                    Timestamp = frame.Timestamp,
                    Data = frame.Data
                }),
                PacketStream.Voice);
        }

        private void OnVoiceStop(PlayerConnection player, PacketPlayerVoiceStop stop)
        {
            if (!_config.Features.VoiceChat)
                return;
            if (stop.PlayerId != player.Id)
                return;
            if (!PacketValidation.IsValidVoiceStop(stop))
                return;

            var voice = player.Voice;
            if (voice == null)
                return;
            if (voice.StreamId != stop.StreamId)
                return;

            StopVoice(player, notifyRoom: true);
        }
    }
}
