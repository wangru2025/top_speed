using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        private const int PlayerDataFieldSize = 32;

        public static bool TryReadHeader(byte[] data, out PacketHeader header)
        {
            header = new PacketHeader();
            if (data.Length < 2)
                return false;
            header.Version = data[0];
            header.Command = (Command)data[1];
            return true;
        }

        public static bool TryReadPlayerState(byte[] data, out PacketPlayerState packet)
        {
            packet = new PacketPlayerState();
            if (data.Length < 2 + 4 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.State = (PlayerState)reader.ReadByte();
            return true;
        }

        public static bool TryReadRacePlayerState(byte[] data, out PacketRacePlayerState packet)
        {
            packet = new PacketRacePlayerState();
            if (data.Length < 2 + 4 + 4 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RaceInstanceId = reader.ReadUInt32();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            packet.State = (PlayerState)reader.ReadByte();
            return true;
        }

        public static bool TryReadPlayer(byte[] data, out PacketPlayer packet)
        {
            packet = new PacketPlayer();
            if (data.Length < 2 + 4 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            return true;
        }

        public static bool TryReadRacePlayer(byte[] data, out PacketRacePlayer packet)
        {
            packet = new PacketRacePlayer();
            if (data.Length < 2 + 4 + 4 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RaceInstanceId = reader.ReadUInt32();
            packet.PlayerId = reader.ReadUInt32();
            packet.PlayerNumber = reader.ReadByte();
            return true;
        }

        public static bool TryReadPlayerHello(byte[] data, out PacketPlayerHello packet)
        {
            packet = new PacketPlayerHello();
            if (data.Length < 2 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Name = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
            return true;
        }

        public static bool TryReadProtocolMessage(byte[] data, out PacketProtocolMessage packet)
        {
            packet = new PacketProtocolMessage();
            if (data.Length < 2 + 1 + ProtocolConstants.MaxProtocolMessageLength)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.ProtocolMessage)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Code = (ProtocolMessageCode)reader.ReadByte();
            packet.Message = reader.ReadFixedString(ProtocolConstants.MaxProtocolMessageLength);
            return PacketValidation.IsValidProtocolMessage(packet);
        }

        public static byte[] WritePacketHeader(Command command, int payloadSize)
        {
            var buffer = new byte[2 + payloadSize];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)command;
            return buffer;
        }

        public static byte[] WritePlayerNumber(uint id, byte playerNumber)
        {
            return WritePlayer(Command.PlayerNumber, id, playerNumber);
        }

        public static byte[] WritePlayer(Command command, uint id, byte playerNumber)
        {
            var buffer = WritePacketHeader(command, 4 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(id);
            writer.WriteByte(playerNumber);
            return buffer;
        }

        public static byte[] WritePlayerState(Command command, uint id, byte playerNumber, PlayerState state)
        {
            var buffer = WritePacketHeader(command, 4 + 1 + 1);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)command);
            writer.WriteUInt32(id);
            writer.WriteByte(playerNumber);
            writer.WriteByte((byte)state);
            return buffer;
        }

        public static byte[] WriteServerInfo(PacketServerInfo info)
        {
            var buffer = WritePacketHeader(Command.ServerInfo, ProtocolConstants.MaxMotdLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ServerInfo);
            writer.WriteFixedString(info.Motd ?? string.Empty, ProtocolConstants.MaxMotdLength);
            return buffer;
        }

        public static byte[] WriteProtocolMessage(PacketProtocolMessage message)
        {
            var buffer = WritePacketHeader(Command.ProtocolMessage, 1 + ProtocolConstants.MaxProtocolMessageLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.ProtocolMessage);
            writer.WriteByte((byte)message.Code);
            writer.WriteFixedString(message.Message ?? string.Empty, ProtocolConstants.MaxProtocolMessageLength);
            return buffer;
        }

        public static byte[] WriteGeneral(Command command)
        {
            var buffer = WritePacketHeader(command, 0);
            return buffer;
        }

        public static byte[] WriteDisconnect(string message)
        {
            var buffer = WritePacketHeader(Command.Disconnect, ProtocolConstants.MaxProtocolDetailsLength);
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.Disconnect);
            writer.WriteFixedString(message ?? string.Empty, ProtocolConstants.MaxProtocolDetailsLength);
            return buffer;
        }

        private static void WritePlayerDataFields(ref PacketWriter writer, PacketPlayerData data)
        {
            writer.WriteUInt32(data.PlayerId);
            writer.WriteByte(data.PlayerNumber);
            writer.WriteByte((byte)data.Car);
            writer.WriteSingle(data.RaceData.PositionX);
            writer.WriteSingle(data.RaceData.PositionY);
            writer.WriteUInt16(data.RaceData.Speed);
            writer.WriteInt32(data.RaceData.Frequency);
            writer.WriteByte((byte)data.State);
            writer.WriteBool(data.EngineRunning);
            writer.WriteBool(data.Braking);
            writer.WriteBool(data.Horning);
            writer.WriteBool(data.Backfiring);
            writer.WriteBool(data.MediaLoaded);
            writer.WriteBool(data.MediaPlaying);
            writer.WriteUInt32(data.MediaId);
            writer.WriteByte(data.RadioVolumePercent);
        }
    }
}
