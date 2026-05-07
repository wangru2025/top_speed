using System;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class RoomStoreSequenceBehaviorTests
{
    [Fact]
    public void TryApplyCurrentRoomEvent_ShouldNotBeBlockedByNewerRoomStateSequence()
    {
        var store = new RoomStore();
        store.ApplyRoomState(new PacketRoomState
        {
            InRoom = true,
            RoomId = 10,
            RoomVersion = 2,
            EventSequence = 12,
            HostPlayerId = 1,
            RoomName = "Room",
            RoomType = GameRoomType.PlayersRace,
            PlayersToStart = 8,
            RaceState = RoomRaceState.Racing,
            Players = Array.Empty<PacketRoomPlayer>()
        });

        var applied = store.TryApplyCurrentRoomEvent(
            new RoomEventInfo
            {
                RoomId = 10,
                RoomVersion = 2,
                EventSequence = 6,
                RaceInstanceId = 1,
                Kind = RoomEventKind.HostChanged,
                HostPlayerId = 2,
                RoomType = GameRoomType.PlayersRace,
                PlayerCount = 2,
                PlayersToStart = 8,
                RaceState = RoomRaceState.Racing,
                RoomName = "Room"
            },
            localPlayerId: 2,
            out var localHostChanged);

        applied.Should().BeTrue();
        localHostChanged.Should().BeTrue();
        store.CurrentRoom.HostPlayerId.Should().Be(2u);
        store.CurrentRoom.IsHost.Should().BeTrue();
    }

    [Fact]
    public void TryApplyCurrentRoomEvent_ShouldNotTreatNewRoomEventsAsStaleAfterRoomTransition()
    {
        var store = new RoomStore();
        store.ApplyRoomState(new PacketRoomState
        {
            InRoom = true,
            RoomId = 1,
            RoomVersion = 10,
            EventSequence = 40,
            HostPlayerId = 1,
            RoomName = "Room 1",
            RoomType = GameRoomType.PlayersRace,
            PlayersToStart = 2,
            RaceState = RoomRaceState.Lobby,
            Players = Array.Empty<PacketRoomPlayer>()
        });

        store.TryApplyCurrentRoomEvent(
                new RoomEventInfo
                {
                    RoomId = 1,
                    RoomVersion = 10,
                    EventSequence = 45,
                    RaceInstanceId = 0,
                    Kind = RoomEventKind.RoomSummaryUpdated,
                    HostPlayerId = 1,
                    RoomType = GameRoomType.PlayersRace,
                    PlayerCount = 1,
                    PlayersToStart = 2,
                    RaceState = RoomRaceState.Lobby,
                    RoomName = "Room 1"
                },
                localPlayerId: 1,
                out _)
            .Should()
            .BeTrue();

        store.ApplyRoomState(new PacketRoomState
        {
            InRoom = false,
            RoomVersion = 0,
            EventSequence = 0,
            HostPlayerId = 0,
            RoomType = GameRoomType.BotsRace,
            PlayersToStart = 0,
            RaceState = RoomRaceState.Lobby,
            Players = Array.Empty<PacketRoomPlayer>()
        });

        store.ApplyRoomState(new PacketRoomState
        {
            InRoom = true,
            RoomId = 2,
            RoomVersion = 1,
            EventSequence = 1,
            HostPlayerId = 2,
            RoomName = "Room 2",
            RoomType = GameRoomType.PlayersRace,
            PlayersToStart = 2,
            RaceState = RoomRaceState.Lobby,
            Players = Array.Empty<PacketRoomPlayer>()
        });

        var applied = store.TryApplyCurrentRoomEvent(
            new RoomEventInfo
            {
                RoomId = 2,
                RoomVersion = 2,
                EventSequence = 2,
                RaceInstanceId = 0,
                Kind = RoomEventKind.ParticipantJoined,
                SubjectPlayerId = 22,
                SubjectPlayerNumber = 1,
                SubjectPlayerState = PlayerState.NotReady,
                SubjectPlayerName = "player-22",
                HostPlayerId = 2,
                RoomType = GameRoomType.PlayersRace,
                PlayerCount = 2,
                PlayersToStart = 2,
                RaceState = RoomRaceState.Lobby,
                RoomName = "Room 2"
            },
            localPlayerId: 2,
            out _);

        applied.Should().BeTrue();
        store.CurrentRoom.RoomId.Should().Be(2u);
        store.CurrentRoom.Players.Should().ContainSingle(p => p.PlayerId == 22u);
    }
}
