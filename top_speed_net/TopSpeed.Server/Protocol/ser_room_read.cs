using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Protocol
{
    internal static partial class PacketSerializer
    {
        public static bool TryReadRoomCreate(byte[] data, out PacketRoomCreate packet)
        {
            packet = new PacketRoomCreate();
            if (data.Length < 2 + ProtocolConstants.MaxRoomNameLength + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
            packet.RoomType = (GameRoomType)reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            return PacketValidation.IsValidRoomCreate(packet);
        }

        public static bool TryReadRoomJoin(byte[] data, out PacketRoomJoin packet)
        {
            packet = new PacketRoomJoin();
            if (data.Length < 2 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            return PacketValidation.IsValidRoomJoin(packet);
        }

        public static bool TryReadRoomGetRequest(byte[] data, out PacketRoomGetRequest packet)
        {
            packet = new PacketRoomGetRequest();
            if (data.Length < 2 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            return PacketValidation.IsValidRoomGetRequest(packet);
        }

        public static bool TryReadRoomSetTrack(byte[] data, out PacketRoomSetTrack packet)
        {
            packet = new PacketRoomSetTrack();
            if (data.Length < 2 + 12)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.TrackName = reader.ReadFixedString(12);
            return PacketValidation.IsValidRoomSetTrack(packet);
        }

        public static bool TryReadRoomSetLaps(byte[] data, out PacketRoomSetLaps packet)
        {
            packet = new PacketRoomSetLaps();
            if (data.Length < 2 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Laps = reader.ReadByte();
            return PacketValidation.IsValidRoomSetLaps(packet);
        }

        public static bool TryReadRoomSetPlayersToStart(byte[] data, out PacketRoomSetPlayersToStart packet)
        {
            packet = new PacketRoomSetPlayersToStart();
            if (data.Length < 2 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            return packet.PlayersToStart >= 2 && packet.PlayersToStart <= ProtocolConstants.MaxRoomPlayersToStart;
        }

        public static bool TryReadRoomSetGameRules(byte[] data, out PacketRoomSetGameRules packet)
        {
            packet = new PacketRoomSetGameRules();
            if (data.Length < 2 + 4)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.GameRulesFlags = reader.ReadUInt32();
            return true;
        }

        public static bool TryReadRoomPlayerReady(byte[] data, out PacketRoomPlayerReady packet)
        {
            packet = new PacketRoomPlayerReady();
            if (data.Length < 2 + 1 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Car = (CarType)reader.ReadByte();
            packet.AutomaticTransmission = reader.ReadBool();
            return PacketValidation.IsValidRoomPlayerReady(packet);
        }

        public static bool TryReadRoomRaceControl(byte[] data, out PacketRoomRaceControl packet)
        {
            packet = new PacketRoomRaceControl();
            if (data.Length < 2 + 1)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.Action = (RoomRaceControlAction)reader.ReadByte();
            return PacketValidation.IsValidRoomRaceControl(packet);
        }

        public static bool TryReadRoomEvent(byte[] data, out PacketRoomEvent packet)
        {
            packet = new PacketRoomEvent();
            if (data.Length < 2 + 4 + 4 + 4 + 4 + 1 + 4 + 1 + 1 + 1 + 1 + 1 + 12 + 1 + 4 + ProtocolConstants.MaxRoomNameLength + 4 + 1 + 1 + ProtocolConstants.MaxPlayerNameLength)
                return false;
            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.RoomId = reader.ReadUInt32();
            packet.RoomVersion = reader.ReadUInt32();
            packet.EventSequence = reader.ReadUInt32();
            packet.RaceInstanceId = reader.ReadUInt32();
            packet.Kind = (RoomEventKind)reader.ReadByte();
            packet.HostPlayerId = reader.ReadUInt32();
            packet.RoomType = (GameRoomType)reader.ReadByte();
            packet.PlayerCount = reader.ReadByte();
            packet.PlayersToStart = reader.ReadByte();
            packet.RaceState = (RoomRaceState)reader.ReadByte();
            packet.RacePaused = reader.ReadBool();
            packet.TrackName = reader.ReadFixedString(12);
            packet.Laps = reader.ReadByte();
            packet.GameRulesFlags = reader.ReadUInt32();
            packet.RoomName = reader.ReadFixedString(ProtocolConstants.MaxRoomNameLength);
            packet.SubjectPlayerId = reader.ReadUInt32();
            packet.SubjectPlayerNumber = reader.ReadByte();
            packet.SubjectPlayerState = (PlayerState)reader.ReadByte();
            packet.SubjectPlayerName = reader.ReadFixedString(ProtocolConstants.MaxPlayerNameLength);
            return PacketValidation.IsValidRoomEvent(packet);
        }

    }
}
