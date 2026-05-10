using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Session
{
    internal sealed class VoiceSend
    {
        private readonly Sender _sender;
        private bool _active;
        private uint _streamId;

        public VoiceSend(Sender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public bool TrySendStart(
            uint playerId,
            byte playerNumber,
            uint streamId,
            LiveAudioProfile profile,
            ushort frequencyTenths,
            bool pushToTalk)
        {
            if (streamId == 0)
                return false;

            if (_active && _streamId == streamId)
                return true;

            if (_active)
            {
                if (!TrySendStop(playerId, playerNumber, _streamId))
                {
                    // Recover from a stale local active state so transmission does not get stuck forever.
                    _active = false;
                    _streamId = 0;
                }
            }

            var sent = _sender.TrySend(
                ClientPacketSerializer.WritePlayerVoiceStart(
                    playerId,
                    playerNumber,
                    streamId,
                    profile.Codec,
                    profile.SampleRate,
                    profile.Channels,
                    profile.FrameMs,
                    frequencyTenths,
                    pushToTalk),
                PacketStream.Voice,
                PacketDeliveryKind.ReliableOrdered);

            if (!sent)
                return false;

            _active = true;
            _streamId = streamId;
            return true;
        }

        public bool TrySendFrame(uint playerId, byte playerNumber, uint streamId, in LiveOpusFrame frame)
        {
            if (!_active || _streamId != streamId)
                return false;

            return _sender.TrySend(
                ClientPacketSerializer.WritePlayerVoiceFrame(
                    playerId,
                    playerNumber,
                    streamId,
                    frame.Sequence,
                    frame.Timestamp,
                    frame.Payload),
                PacketStream.Voice,
                PacketDeliveryKind.Sequenced);
        }

        public bool TrySendStop(uint playerId, byte playerNumber, uint streamId)
        {
            if (!_active || _streamId != streamId)
                return true;

            var sent = _sender.TrySend(
                ClientPacketSerializer.WritePlayerVoiceStop(playerId, playerNumber, streamId),
                PacketStream.Voice,
                PacketDeliveryKind.ReliableOrdered);

            if (!sent)
                return false;

            _active = false;
            _streamId = 0;
            return true;
        }

        public void Reset()
        {
            _active = false;
            _streamId = 0;
        }
    }
}
