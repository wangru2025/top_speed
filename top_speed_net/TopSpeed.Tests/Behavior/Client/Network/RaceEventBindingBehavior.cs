using TopSpeed.Game;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class RaceEventBindingBehaviorTests
{
    [Fact]
    public void AcceptRaceEvent_ShouldRejectStaleEvent()
    {
        var binding = new MultiplayerRaceBinding
        {
            RoomId = 10,
            RaceInstanceId = 20,
            EventSequence = 5
        };

        binding.AcceptRaceEvent(10, 20, 4, allowBindRaceInstance: false).Should().BeFalse();
        binding.EventSequence.Should().Be(5);
    }

    [Fact]
    public void AcceptRaceEvent_ShouldRejectWrongRaceInstance()
    {
        var binding = new MultiplayerRaceBinding
        {
            RoomId = 10,
            RaceInstanceId = 20,
            EventSequence = 5
        };

        binding.AcceptRaceEvent(10, 21, 6, allowBindRaceInstance: false).Should().BeFalse();
        binding.ShouldRequestResync(10, 21, 6).Should().BeTrue();
        binding.EventSequence.Should().Be(5);
    }

    [Fact]
    public void AcceptRaceEvent_ShouldAdvanceSequenceForCurrentRace()
    {
        var binding = new MultiplayerRaceBinding
        {
            RoomId = 10,
            RaceInstanceId = 20,
            EventSequence = 5
        };

        binding.AcceptRaceEvent(10, 20, 6, allowBindRaceInstance: false).Should().BeTrue();
        binding.EventSequence.Should().Be(6);
    }

    [Fact]
    public void AcceptRaceEvent_ShouldBindRaceInstanceWhenAllowed()
    {
        var binding = new MultiplayerRaceBinding
        {
            RoomId = 10,
            RaceInstanceId = 0,
            EventSequence = 0
        };

        binding.AcceptRaceEvent(10, 20, 1, allowBindRaceInstance: true).Should().BeTrue();
        binding.RaceInstanceId.Should().Be(20);
        binding.EventSequence.Should().Be(1);
    }

    [Fact]
    public void AcceptRaceEvent_ShouldRejectOlderFinishAfterCompletion()
    {
        var binding = new MultiplayerRaceBinding
        {
            RoomId = 10,
            RaceInstanceId = 20,
            EventSequence = 6
        };

        binding.AcceptRaceEvent(10, 20, 7, allowBindRaceInstance: false).Should().BeTrue();
        binding.AcceptRaceEvent(10, 20, 6, allowBindRaceInstance: false).Should().BeFalse();
        binding.EventSequence.Should().Be(7);
    }

    [Fact]
    public void ApplyRoomState_ShouldAdvanceStateSequenceWithoutOverwritingRaceEventSequence()
    {
        var binding = new MultiplayerRaceBinding
        {
            RoomId = 10,
            ActiveRoomId = 10,
            RaceInstanceId = 20,
            EventSequence = 5,
            StateSequence = 4
        };

        binding.ApplyRoomState(new PacketRoomState
        {
            InRoom = true,
            RoomId = 10,
            RaceInstanceId = 20,
            RaceState = RoomRaceState.Racing,
            EventSequence = 12
        });

        binding.EventSequence.Should().Be(5);
        binding.StateSequence.Should().Be(12);
    }

    [Fact]
    public void AcceptRaceEvent_ShouldIgnoreHigherRoomStateSequence()
    {
        var binding = new MultiplayerRaceBinding
        {
            RoomId = 10,
            RaceInstanceId = 20,
            EventSequence = 5,
            StateSequence = 12
        };

        binding.AcceptRaceEvent(10, 20, 6, allowBindRaceInstance: false).Should().BeTrue();
        binding.EventSequence.Should().Be(6);
        binding.StateSequence.Should().Be(12);
    }
}
