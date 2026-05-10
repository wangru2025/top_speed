using System;

namespace TopSpeed.Protocol
{
    public sealed class PacketPlayerVoiceStart
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public uint StreamId;
        public LiveCodec Codec;
        public ushort SampleRate;
        public byte Channels;
        public byte FrameMs;
        public ushort FrequencyTenths;
        public bool PushToTalk;
    }

    public sealed class PacketPlayerVoiceFrame
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public uint StreamId;
        public ushort Sequence;
        public uint Timestamp;
        public byte[] Data = Array.Empty<byte>();
    }

    public sealed class PacketPlayerVoiceStop
    {
        public uint PlayerId;
        public byte PlayerNumber;
        public uint StreamId;
    }
}
