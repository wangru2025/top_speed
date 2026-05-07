using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static bool TryReadProtocolHello(byte[] data, out PacketProtocolHello packet)
        {
            packet = new PacketProtocolHello();
            if (data.Length < 2 + 5 + 5 + 5)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.ClientVersion = ReadProtocolVer(ref reader);
                packet.MinSupported = ReadProtocolVer(ref reader);
                packet.MaxSupported = ReadProtocolVer(ref reader);
                if (data.Length >= 2 + 5 + 5 + 5 + 4)
                    packet.ResumePlayerId = reader.ReadUInt32();
                if (data.Length >= 2 + 5 + 5 + 5 + 4 + 8)
                    packet.ResumeToken = reader.ReadUInt64();
                return true;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                packet = new PacketProtocolHello();
                return false;
            }
        }

        public static byte[] WriteProtocolWelcome(PacketProtocolWelcome packet)
        {
            var buffer = WritePacketHeader(Command.ProtocolWelcome, 1 + 5 + 5 + 5 + ProtocolConstants.MaxProtocolDetailsLength + 8);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ProtocolWelcome);
            writer.WriteByte((byte)packet.Status);
            WriteProtocolVer(ref writer, packet.NegotiatedVersion);
            WriteProtocolVer(ref writer, packet.ServerMinSupported);
            WriteProtocolVer(ref writer, packet.ServerMaxSupported);
            writer.WriteFixedString(packet.Message ?? string.Empty, ProtocolConstants.MaxProtocolDetailsLength);
            writer.WriteUInt64(packet.ResumeToken);
            return buffer;
        }

        public static bool TryReadClientHeartbeat(byte[] data, out PacketClientHeartbeat packet)
        {
            packet = new PacketClientHeartbeat();
            if (data == null || data.Length < 2 + 4 + 8 + 4 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ClientHeartbeat)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.SessionId = reader.ReadUInt64();
            packet.ClientTick = reader.ReadUInt32();
            packet.LastReceivedServerTick = reader.ReadUInt32();
            return true;
        }

        public static byte[] WriteServerHeartbeat(PacketServerHeartbeat packet)
        {
            var buffer = WritePacketHeader(Command.ServerHeartbeat, 4 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ServerHeartbeat);
            writer.WriteUInt32(packet.ServerTick);
            writer.WriteUInt32(packet.LastReceivedClientTick);
            return buffer;
        }

        private static ProtocolVer ReadProtocolVer(ref PacketReader reader)
        {
            var year = reader.ReadUInt16();
            var month = reader.ReadByte();
            var day = reader.ReadByte();
            var revision = reader.ReadByte();
            return new ProtocolVer(year, month, day, revision);
        }

        private static void WriteProtocolVer(ref PacketWriter writer, ProtocolVer version)
        {
            writer.WriteUInt16(version.Year);
            writer.WriteByte(version.Month);
            writer.WriteByte(version.Day);
            writer.WriteByte(version.Revision);
        }
    }
}
