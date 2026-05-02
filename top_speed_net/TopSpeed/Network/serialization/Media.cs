using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
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

        public static byte[] WritePlayerMediaBegin(uint playerId, byte playerNumber, uint mediaId, uint totalBytes, string fileExtension)
        {
            return WritePlayerMediaBegin(playerId, playerNumber, mediaId, mediaId, totalBytes, fileExtension);
        }

        public static byte[] WritePlayerMediaBegin(uint playerId, byte playerNumber, uint mediaId, uint transferId, uint totalBytes, string fileExtension)
        {
            var buffer = WritePacketHeader(Command.PlayerMediaBegin, 4 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerMediaBegin);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(mediaId);
            writer.WriteUInt32(transferId);
            writer.WriteUInt32(totalBytes);
            writer.WriteFixedString(fileExtension ?? string.Empty, ProtocolConstants.MaxMediaFileExtensionLength);
            return buffer;
        }

        public static byte[] WritePlayerMediaChunk(uint playerId, byte playerNumber, uint mediaId, ushort chunkIndex, byte[] data)
        {
            return WritePlayerMediaChunk(playerId, playerNumber, mediaId, mediaId, chunkIndex, data);
        }

        public static byte[] WritePlayerMediaChunk(uint playerId, byte playerNumber, uint mediaId, uint transferId, ushort chunkIndex, byte[] data)
        {
            var bytes = data ?? Array.Empty<byte>();
            if (bytes.Length > ProtocolConstants.MaxMediaChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(data), $"Media chunk cannot exceed {ProtocolConstants.MaxMediaChunkBytes} bytes.");

            var buffer = WritePacketHeader(Command.PlayerMediaChunk, 4 + 1 + 4 + 4 + 2 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerMediaChunk);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(mediaId);
            writer.WriteUInt32(transferId);
            writer.WriteUInt16(chunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        public static byte[] WritePlayerMediaEnd(uint playerId, byte playerNumber, uint mediaId)
        {
            return WritePlayerMediaEnd(playerId, playerNumber, mediaId, mediaId);
        }

        public static byte[] WritePlayerMediaEnd(uint playerId, byte playerNumber, uint mediaId, uint transferId)
        {
            var buffer = WritePacketHeader(Command.PlayerMediaEnd, 4 + 1 + 4 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerMediaEnd);
            writer.WriteUInt32(playerId);
            writer.WriteByte(playerNumber);
            writer.WriteUInt32(mediaId);
            writer.WriteUInt32(transferId);
            return buffer;
        }
    }
}

