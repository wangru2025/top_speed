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
}
