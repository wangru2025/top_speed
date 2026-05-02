using TopSpeed.Protocol;
using TopSpeed.Data;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class RaceResultBehaviorTests
{
    [Theory]
    [InlineData(RoomRaceResultStatus.Finished, true)]
    [InlineData(RoomRaceResultStatus.Dnf, true)]
    [InlineData(RoomRaceResultStatus.Pending, false)]
    [InlineData(RoomRaceResultStatus.None, false)]
    public void TerminalStatuses_ShouldBeExplicit(RoomRaceResultStatus status, bool expected)
    {
        RaceResultRules.IsTerminal(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(RoomRaceResultStatus.Finished, RoomRaceResultStatus.Finished)]
    [InlineData(RoomRaceResultStatus.Dnf, RoomRaceResultStatus.Dnf)]
    [InlineData(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Dnf)]
    [InlineData(RoomRaceResultStatus.None, RoomRaceResultStatus.Dnf)]
    public void CompletionStatuses_ShouldNormalizeUnresolvedEntriesToDnf(RoomRaceResultStatus input, RoomRaceResultStatus expected)
    {
        RaceResultRules.NormalizeCompletionStatus(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(RoomRaceResultStatus.Finished, RoomRaceResultStatus.Dnf, RoomRaceResultStatus.Finished)]
    [InlineData(RoomRaceResultStatus.Dnf, RoomRaceResultStatus.Finished, RoomRaceResultStatus.Finished)]
    [InlineData(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Dnf, RoomRaceResultStatus.Dnf)]
    [InlineData(RoomRaceResultStatus.None, RoomRaceResultStatus.Pending, RoomRaceResultStatus.Pending)]
    public void ParticipantStatusResolution_ShouldUseSingleAuthoritativePolicy(
        RoomRaceResultStatus current,
        RoomRaceResultStatus requested,
        RoomRaceResultStatus expected)
    {
        RaceResultRules.ResolveParticipantStatus(current, requested).Should().Be(expected);
    }

    [Fact]
    public void RaceCompletion_ShouldWaitUntilAllParticipantsAreTerminal()
    {
        RaceResultRules.ShouldComplete(new[]
        {
            RoomRaceResultStatus.Finished,
            RoomRaceResultStatus.Pending
        }).Should().BeFalse();

        RaceResultRules.ShouldComplete(new[]
        {
            RoomRaceResultStatus.Finished,
            RoomRaceResultStatus.Dnf
        }).Should().BeTrue();
    }

    [Fact]
    public void RaceCompletion_ShouldHandleAllPlayersFinished()
    {
        RaceResultRules.ShouldComplete(new[]
        {
            RoomRaceResultStatus.Finished,
            RoomRaceResultStatus.Finished,
            RoomRaceResultStatus.Finished
        }).Should().BeTrue();
    }

    [Fact]
    public void RaceCompletion_ShouldHandleDisconnectedAfterGraceAsDnf()
    {
        var disconnected = RaceResultRules.ResolveParticipantStatus(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Dnf);

        RaceResultRules.ShouldComplete(new[]
        {
            RoomRaceResultStatus.Finished,
            disconnected
        }).Should().BeTrue();
    }

    [Fact]
    public void RaceCompletion_ShouldNotDnfPlayerWhoReconnectsBeforeGraceExpiry()
    {
        var resumed = RaceResultRules.ResolveParticipantStatus(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Pending);

        RaceResultRules.ShouldComplete(new[]
        {
            RoomRaceResultStatus.Finished,
            resumed
        }).Should().BeFalse();
    }

    [Fact]
    public void RaceCompletion_ShouldHandleHostDisconnectingMidRaceAfterGrace()
    {
        var host = RaceResultRules.ResolveParticipantStatus(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Dnf);
        var guest = RaceResultRules.ResolveParticipantStatus(RoomRaceResultStatus.Pending, RoomRaceResultStatus.Finished);

        RaceResultRules.ShouldComplete(new[] { host, guest }).Should().BeTrue();
    }

    [Fact]
    public void RaceCompletion_ShouldTreatAbortAsDnfForUnresolvedParticipants()
    {
        RaceResultRules.NormalizeCompletionStatus(RoomRaceResultStatus.Pending).Should().Be(RoomRaceResultStatus.Dnf);
        RaceResultRules.NormalizeCompletionStatus(RoomRaceResultStatus.None).Should().Be(RoomRaceResultStatus.Dnf);
    }

    [Fact]
    public void RaceTrackFinishDistance_ShouldUseConfiguredLaps()
    {
        var track = TrackCatalog.BuiltIn["america"];
        var lap = RaceDistanceRules.CalculateLapDistance(track.Definitions);
        var distance = RaceDistanceRules.CalculateRaceDistance(track.Definitions, roomLaps: 3, trackLaps: track.Laps);

        lap.Should().BeGreaterThan(0f);
        distance.Should().BeApproximately(lap * 3, 0.001f);
        RaceDistanceRules.HasCrossedFinish(distance - 0.1f, distance).Should().BeFalse();
        RaceDistanceRules.HasCrossedFinish(distance, distance).Should().BeTrue();
    }

    [Fact]
    public void StreetAdventureFinishDistance_ShouldUseFullAdventureDistance()
    {
        var track = TrackCatalog.BuiltIn["advHills"];
        var lap = RaceDistanceRules.CalculateLapDistance(track.Definitions);
        var distance = RaceDistanceRules.CalculateRaceDistance(track.Definitions, roomLaps: 1, trackLaps: track.Laps);

        lap.Should().BeGreaterThan(0f);
        distance.Should().BeApproximately(lap, 0.001f);
        RaceDistanceRules.HasCrossedFinish(float.NaN, distance).Should().BeFalse();
        RaceDistanceRules.HasCrossedFinish(distance + 0.1f, distance).Should().BeTrue();
    }

    [Theory]
    [InlineData(GameRoomType.BotsRace, 10, 10)]
    [InlineData(GameRoomType.PlayersRace, 0, 2)]
    [InlineData(GameRoomType.PlayersRace, 99, 2)]
    [InlineData(GameRoomType.OneOnOne, 10, 2)]
    public void RoomPlayerLimits_ShouldNormalizeByRoomType(GameRoomType roomType, byte input, byte expected)
    {
        RoomRules.NormalizePlayersToStart(roomType, input).Should().Be(expected);
    }

    [Fact]
    public void InvalidRoomPacketEnums_ShouldNormalizeToSafeStates()
    {
        RoomRules.NormalizeType((GameRoomType)255).Should().Be(GameRoomType.BotsRace);
        RoomRules.NormalizeRaceState((RoomRaceState)255).Should().Be(RoomRaceState.Lobby);
        RoomRules.NormalizeParticipantState((PlayerState)255).Should().Be(PlayerState.NotReady);
    }
}
