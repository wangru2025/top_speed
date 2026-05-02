using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class RoomEventJournalEntry
    {
        public uint Sequence { get; set; }
        public uint RaceInstanceId { get; set; }
        public Command Command { get; set; }
        public PacketStream Stream { get; set; }
        public byte[] Payload { get; set; } = Array.Empty<byte>();
    }
}
