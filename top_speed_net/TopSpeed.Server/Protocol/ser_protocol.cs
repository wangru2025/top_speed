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
