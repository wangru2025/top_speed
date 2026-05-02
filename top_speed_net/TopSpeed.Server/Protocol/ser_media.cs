using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static bool TryReadPlayerMediaBegin(byte[] data, out PacketPlayerMediaBegin packet)
        {
            packet = new PacketPlayerMediaBegin();
            if (data.Length < 2 + 4 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.TransferId = reader.ReadUInt32();
            packet.TotalBytes = reader.ReadUInt32();
            packet.FileExtension = reader.ReadFixedString(ProtocolConstants.MaxMediaFileExtensionLength);
            return true;
        }

        public static bool TryReadPlayerMediaChunk(byte[] data, out PacketPlayerMediaChunk packet)
        {
            packet = new PacketPlayerMediaChunk();
            if (data.Length < 2 + 4 + 1 + 4 + 4 + 2 + 2)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.TransferId = reader.ReadUInt32();
            packet.ChunkIndex = reader.ReadUInt16();
            var length = reader.ReadUInt16();
            if (length > ProtocolConstants.MaxMediaChunkBytes)
                return false;
            if (data.Length != 2 + 4 + 1 + 4 + 4 + 2 + 2 + length)
                return false;
            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
                bytes[i] = reader.ReadByte();
            packet.Data = bytes;
            return true;
        }

        public static bool TryReadPlayerMediaEnd(byte[] data, out PacketPlayerMediaEnd packet)
        {
            packet = new PacketPlayerMediaEnd();
            if (data.Length < 2 + 4 + 1 + 4 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.TransferId = reader.ReadUInt32();
            return true;
        }

        public static byte[] WritePlayerMediaBegin(PacketPlayerMediaBegin media)
        {
            var buffer = WritePacketHeader(Command.PlayerMediaBegin, 4 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerMediaBegin);
            writer.WriteUInt32(media.PlayerId);
            writer.WriteByte(media.PlayerNumber);
            writer.WriteUInt32(media.MediaId);
            writer.WriteUInt32(media.TransferId);
            writer.WriteUInt32(media.TotalBytes);
            writer.WriteFixedString(media.FileExtension ?? string.Empty, ProtocolConstants.MaxMediaFileExtensionLength);
            return buffer;
        }

        public static byte[] WritePlayerMediaChunk(PacketPlayerMediaChunk media)
        {
            var bytes = media.Data ?? Array.Empty<byte>();
            if (bytes.Length > ProtocolConstants.MaxMediaChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(media), $"Media chunk cannot exceed {ProtocolConstants.MaxMediaChunkBytes} bytes.");

            var buffer = WritePacketHeader(Command.PlayerMediaChunk, 4 + 1 + 4 + 4 + 2 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerMediaChunk);
            writer.WriteUInt32(media.PlayerId);
            writer.WriteByte(media.PlayerNumber);
            writer.WriteUInt32(media.MediaId);
            writer.WriteUInt32(media.TransferId);
            writer.WriteUInt16(media.ChunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WritePlayerMediaEnd(PacketPlayerMediaEnd media)
        {
            var buffer = WritePacketHeader(Command.PlayerMediaEnd, 4 + 1 + 4 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerMediaEnd);
            writer.WriteUInt32(media.PlayerId);
            writer.WriteByte(media.PlayerNumber);
            writer.WriteUInt32(media.MediaId);
            writer.WriteUInt32(media.TransferId);
            return buffer;
        }
    }
}
