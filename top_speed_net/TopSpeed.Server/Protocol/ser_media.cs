using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static bool TryReadPlayerMediaBegin(byte[] data, out PacketPlayerMediaBegin packet)
        {
            return TryReadPlayerMediaBeginCore(data, out packet, expectsFrequency: false);
        }

        public static bool TryReadPlayerCommunicatorMediaBegin(byte[] data, out PacketPlayerMediaBegin packet)
        {
            return TryReadPlayerMediaBeginCore(data, out packet, expectsFrequency: true);
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

        public static bool TryReadPlayerCommunicatorMediaChunk(byte[] data, out PacketPlayerMediaChunk packet)
        {
            return TryReadPlayerMediaChunk(data, out packet);
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

        public static bool TryReadPlayerCommunicatorMediaEnd(byte[] data, out PacketPlayerMediaEnd packet)
        {
            return TryReadPlayerMediaEnd(data, out packet);
        }

        public static bool TryReadPlayerCommunicatorMediaState(byte[] data, out PacketPlayerCommunicatorMediaState packet)
        {
            packet = new PacketPlayerCommunicatorMediaState();
            if (data.Length < 2 + 4 + 1 + 4 + 2 + 1 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.MediaId = reader.ReadUInt32();
            packet.FrequencyTenths = reader.ReadUInt16();
            packet.MediaLoaded = reader.ReadBool();
            packet.MediaPlaying = reader.ReadBool();
            packet.VolumePercent = reader.ReadByte();
            return true;
        }

        public static byte[] WritePlayerMediaBegin(PacketPlayerMediaBegin media)
        {
            return WritePlayerMediaBeginCore(
                Command.PlayerMediaBegin,
                media,
                includeFrequency: false);
        }

        public static byte[] WritePlayerCommunicatorMediaBegin(PacketPlayerMediaBegin media)
        {
            return WritePlayerMediaBeginCore(
                Command.PlayerCommunicatorMediaBegin,
                media,
                includeFrequency: true);
        }

        public static byte[] WritePlayerMediaChunk(PacketPlayerMediaChunk media)
        {
            return WritePlayerMediaChunkCore(Command.PlayerMediaChunk, media);
        }

        public static byte[] WritePlayerCommunicatorMediaChunk(PacketPlayerMediaChunk media)
        {
            return WritePlayerMediaChunkCore(Command.PlayerCommunicatorMediaChunk, media);
        }

        public static byte[] WritePlayerMediaEnd(PacketPlayerMediaEnd media)
        {
            return WritePlayerMediaEndCore(Command.PlayerMediaEnd, media);
        }

        public static byte[] WritePlayerCommunicatorMediaEnd(PacketPlayerMediaEnd media)
        {
            return WritePlayerMediaEndCore(Command.PlayerCommunicatorMediaEnd, media);
        }

        public static byte[] WritePlayerCommunicatorMediaState(PacketPlayerCommunicatorMediaState state)
        {
            var packet = state ?? new PacketPlayerCommunicatorMediaState();
            var buffer = WritePacketHeader(Command.PlayerCommunicatorMediaState, 4 + 1 + 4 + 2 + 1 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerCommunicatorMediaState);
            writer.WriteUInt32(packet.PlayerId);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteUInt32(packet.MediaId);
            writer.WriteUInt16(packet.FrequencyTenths);
            writer.WriteBool(packet.MediaLoaded);
            writer.WriteBool(packet.MediaPlaying);
            writer.WriteByte(packet.VolumePercent);
            return buffer;
        }

        private static bool TryReadPlayerMediaBeginCore(byte[] data, out PacketPlayerMediaBegin packet, bool expectsFrequency)
        {
            packet = new PacketPlayerMediaBegin();
            var minLength = 2 + 4 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength + (expectsFrequency ? 2 : 0);
            if (data.Length < minLength)
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
            if (expectsFrequency)
                packet.FrequencyTenths = reader.ReadUInt16();
            return true;
        }

        private static byte[] WritePlayerMediaBeginCore(Command command, PacketPlayerMediaBegin media, bool includeFrequency)
        {
            var packet = media ?? new PacketPlayerMediaBegin();
            var payloadSize = 4 + 1 + 4 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength + (includeFrequency ? 2 : 0);
            var buffer = WritePacketHeader(command, payloadSize);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(packet.PlayerId);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteUInt32(packet.MediaId);
            writer.WriteUInt32(packet.TransferId);
            writer.WriteUInt32(packet.TotalBytes);
            writer.WriteFixedString(packet.FileExtension ?? string.Empty, ProtocolConstants.MaxMediaFileExtensionLength);
            if (includeFrequency)
                writer.WriteUInt16(packet.FrequencyTenths);
            return buffer;
        }

        private static byte[] WritePlayerMediaChunkCore(Command command, PacketPlayerMediaChunk media)
        {
            var packet = media ?? new PacketPlayerMediaChunk();
            var bytes = packet.Data ?? Array.Empty<byte>();
            if (bytes.Length > ProtocolConstants.MaxMediaChunkBytes)
                throw new ArgumentOutOfRangeException(nameof(media), $"Media chunk cannot exceed {ProtocolConstants.MaxMediaChunkBytes} bytes.");

            var buffer = WritePacketHeader(command, 4 + 1 + 4 + 4 + 2 + 2 + bytes.Length);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(packet.PlayerId);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteUInt32(packet.MediaId);
            writer.WriteUInt32(packet.TransferId);
            writer.WriteUInt16(packet.ChunkIndex);
            writer.WriteUInt16((ushort)bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
                writer.WriteByte(bytes[i]);
            return buffer;
        }

        private static byte[] WritePlayerMediaEndCore(Command command, PacketPlayerMediaEnd media)
        {
            var packet = media ?? new PacketPlayerMediaEnd();
            var buffer = WritePacketHeader(command, 4 + 1 + 4 + 4);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(packet.PlayerId);
            writer.WriteByte(packet.PlayerNumber);
            writer.WriteUInt32(packet.MediaId);
            writer.WriteUInt32(packet.TransferId);
            return buffer;
        }
    }
}
