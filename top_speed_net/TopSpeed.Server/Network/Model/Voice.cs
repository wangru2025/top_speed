using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class VoiceState
    {
        public uint StreamId { get; set; }
        public LiveCodec Codec { get; set; }
        public ushort SampleRate { get; set; }
        public byte Channels { get; set; }
        public byte FrameMs { get; set; }
        public ushort FrequencyTenths { get; set; }
        public bool PushToTalk { get; set; }
        public ushort NextSequence { get; set; }
        public bool HasSequence { get; set; }
        public DateTime LastFrameUtc { get; set; }
    }
}
