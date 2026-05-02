using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadProtocolWelcome(byte[] data, out PacketProtocolWelcome packet)
        {
            packet = new PacketProtocolWelcome();
            if (data.Length < 2 + 1 + 5 + 5 + 5 + ProtocolConstants.MaxProtocolDetailsLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ProtocolWelcome)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Status = (ProtocolCompatStatus)reader.ReadByte();
                packet.NegotiatedVersion = ReadProtocolVer(ref reader);
                packet.ServerMinSupported = ReadProtocolVer(ref reader);
                packet.ServerMaxSupported = ReadProtocolVer(ref reader);
                packet.Message = reader.ReadFixedString(ProtocolConstants.MaxProtocolDetailsLength);
                if (data.Length >= 2 + 1 + 5 + 5 + 5 + ProtocolConstants.MaxProtocolDetailsLength + 8)
                    packet.ResumeToken = reader.ReadUInt64();
                return true;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                packet = new PacketProtocolWelcome();
                return false;
            }
        }

        public static byte[] WriteProtocolHello(PacketProtocolHello packet)
        {
            var buffer = WritePacketHeader(Command.ProtocolHello, 5 + 5 + 5 + 4 + 8);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ProtocolHello);
            WriteProtocolVer(ref writer, packet.ClientVersion);
            WriteProtocolVer(ref writer, packet.MinSupported);
            WriteProtocolVer(ref writer, packet.MaxSupported);
            writer.WriteUInt32(packet.ResumePlayerId);
            writer.WriteUInt64(packet.ResumeToken);
            return buffer;
        }

        public static byte[] WriteProtocolMessage(PacketProtocolMessage packet)
        {
            var buffer = WritePacketHeader(Command.ProtocolMessage, 1 + ProtocolConstants.MaxProtocolMessageLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ProtocolMessage);
            writer.WriteByte((byte)packet.Code);
            writer.WriteFixedString(packet.Message ?? string.Empty, ProtocolConstants.MaxProtocolMessageLength);
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

