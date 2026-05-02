using System.Linq;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Tests;

internal static class ProtocolHarness
{
    public static object BuildSnapshot()
    {
        var playerStatePayload = ClientPacketSerializer.WriteRacePlayerState(
            Command.PlayerState,
            raceInstanceId: 42u,
            playerId: 7u,
            playerNumber: 3,
            state: PlayerState.Racing);

        var playerDataPayload = ClientPacketSerializer.WriteRacePlayerDataToServer(
            raceInstanceId: 9001u,
            playerId: 11u,
            playerNumber: 2,
            car: CarType.Vehicle4,
            raceData: new PlayerRaceData
            {
                PositionX = 1.5f,
                PositionY = 300.25f,
                Speed = 188,
                Frequency = 9200
            },
            state: PlayerState.Finished,
            engineRunning: true,
            braking: false,
            horning: true,
            backfiring: false,
            mediaLoaded: true,
            mediaPlaying: true,
            mediaId: 99u,
            radioVolumePercent: 75);

        var roomEvent = new PacketRoomEvent
        {
            RoomId = 12,
            RoomVersion = 5,
            EventSequence = 9,
            RaceInstanceId = 6,
            Kind = RoomEventKind.RoomSummaryUpdated,
            HostPlayerId = 44,
            RoomType = GameRoomType.PlayersRace,
            PlayerCount = 2,
            PlayersToStart = 2,
            RaceState = RoomRaceState.Preparing,
            RacePaused = true,
            TrackName = "desert",
            Laps = 3,
            GameRulesFlags = 17,
            RoomName = "room-a",
            SubjectPlayerId = 7,
            SubjectPlayerNumber = 1,
            SubjectPlayerState = PlayerState.AwaitingStart,
            SubjectPlayerName = "alice"
        };

        var roomEventPayload = ClientPacketSerializer.WriteRoomEvent(roomEvent);
        ClientPacketSerializer.TryReadRoomEvent(roomEventPayload, out var parsedRoomEvent);
        var roomRaceControlPayload = ClientPacketSerializer.WriteRoomRaceControl(RoomRaceControlAction.Pause);
        ClientPacketSerializer.TryReadRoomRaceControl(roomRaceControlPayload, out var parsedRoomRaceControl);

        return new
        {
            PlayerState = new
            {
                Bytes = playerStatePayload.Select(x => (int)x).ToArray(),
                Length = playerStatePayload.Length
            },
            PlayerDataToServer = DecodePlayerData(playerDataPayload),
            RoomEvent = new
            {
                Bytes = roomEventPayload.Select(x => (int)x).ToArray(),
                Parsed = new
                {
                    parsedRoomEvent.RoomId,
                    parsedRoomEvent.RoomVersion,
                    parsedRoomEvent.EventSequence,
                    parsedRoomEvent.RaceInstanceId,
                    parsedRoomEvent.Kind,
                    parsedRoomEvent.HostPlayerId,
                    parsedRoomEvent.RoomType,
                    parsedRoomEvent.PlayerCount,
                    parsedRoomEvent.PlayersToStart,
                    parsedRoomEvent.RaceState,
                    parsedRoomEvent.RacePaused,
                    parsedRoomEvent.TrackName,
                    parsedRoomEvent.Laps,
                    parsedRoomEvent.GameRulesFlags,
                    parsedRoomEvent.RoomName,
                    parsedRoomEvent.SubjectPlayerId,
                    parsedRoomEvent.SubjectPlayerNumber,
                    parsedRoomEvent.SubjectPlayerState,
                    parsedRoomEvent.SubjectPlayerName
                }
            },
            RoomRaceControl = new
            {
                Bytes = roomRaceControlPayload.Select(x => (int)x).ToArray(),
                Parsed = parsedRoomRaceControl.Action
            }
        };
    }

    private static object DecodePlayerData(byte[] payload)
    {
        var reader = new PacketReader(payload);
        return new
        {
            Version = reader.ReadByte(),
            Command = (Command)reader.ReadByte(),
            RaceInstanceId = reader.ReadUInt32(),
            PlayerId = reader.ReadUInt32(),
            PlayerNumber = reader.ReadByte(),
            Car = (CarType)reader.ReadByte(),
            PositionX = Rounding.F(reader.ReadSingle()),
            PositionY = Rounding.F(reader.ReadSingle()),
            Speed = reader.ReadUInt16(),
            Frequency = reader.ReadInt32(),
            State = (PlayerState)reader.ReadByte(),
            EngineRunning = reader.ReadBool(),
            Braking = reader.ReadBool(),
            Horning = reader.ReadBool(),
            Backfiring = reader.ReadBool(),
            MediaLoaded = reader.ReadBool(),
            MediaPlaying = reader.ReadBool(),
            MediaId = reader.ReadUInt32(),
            RadioVolumePercent = reader.ReadByte(),
            Length = payload.Length
        };
    }
}
