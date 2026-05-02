using System;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class PacketValidationBehaviorTests
{
    [Fact]
    public void RacePacketIdentity_ShouldRejectInvalidValues()
    {
        PacketValidation.IsValidPlayerId(0).Should().BeFalse();
        PacketValidation.IsValidPlayerId(1).Should().BeTrue();
        PacketValidation.IsValidPlayerNumber((byte)ProtocolConstants.MaxPlayers).Should().BeFalse();
        PacketValidation.IsValidPlayerNumber((byte)(ProtocolConstants.MaxPlayers - 1)).Should().BeTrue();
        PacketValidation.IsValidRaceInstance(0).Should().BeFalse();
        PacketValidation.IsValidRoomId(0).Should().BeFalse();
    }

    [Fact]
    public void RacePacketPositions_ShouldRejectNonFiniteValues()
    {
        PacketValidation.IsFinitePosition(1f, 2f).Should().BeTrue();
        PacketValidation.IsFinitePosition(float.NaN, 2f).Should().BeFalse();
        PacketValidation.IsFinitePosition(1f, float.PositiveInfinity).Should().BeFalse();
    }

    [Fact]
    public void MediaPackets_ShouldRequireTransferIdentityAndSafeSizes()
    {
        PacketValidation.IsValidMediaBegin(new PacketPlayerMediaBegin
        {
            PlayerId = 1,
            PlayerNumber = 0,
            MediaId = 5,
            TransferId = 9,
            TotalBytes = 128
        }).Should().BeTrue();

        PacketValidation.IsValidMediaBegin(new PacketPlayerMediaBegin
        {
            PlayerId = 1,
            PlayerNumber = 0,
            MediaId = 5,
            TransferId = 0,
            TotalBytes = 128
        }).Should().BeFalse();

        PacketValidation.IsValidMediaChunk(new PacketPlayerMediaChunk
        {
            PlayerId = 1,
            PlayerNumber = 0,
            MediaId = 5,
            TransferId = 9,
            ChunkIndex = 0,
            Data = new byte[ProtocolConstants.MaxMediaChunkBytes + 1]
        }).Should().BeFalse();

        PacketValidation.IsValidMediaEnd(new PacketPlayerMediaEnd
        {
            PlayerId = 1,
            PlayerNumber = 0,
            MediaId = 5,
            TransferId = 9
        }).Should().BeTrue();
    }

    [Fact]
    public void LivePackets_ShouldRejectOversizedFramesAndInvalidStart()
    {
        PacketValidation.IsValidLiveStart(new PacketPlayerLiveStart
        {
            PlayerId = 1,
            PlayerNumber = 0,
            StreamId = 5,
            Codec = LiveCodec.Opus,
            SampleRate = ProtocolConstants.LiveSampleRate,
            Channels = 1,
            FrameMs = ProtocolConstants.LiveFrameMs
        }).Should().BeTrue();

        PacketValidation.IsValidLiveStart(new PacketPlayerLiveStart
        {
            PlayerId = 1,
            PlayerNumber = 0,
            StreamId = 5,
            Codec = LiveCodec.None,
            SampleRate = ProtocolConstants.LiveSampleRate,
            Channels = 1,
            FrameMs = ProtocolConstants.LiveFrameMs
        }).Should().BeFalse();

        PacketValidation.IsValidLiveFrame(new PacketPlayerLiveFrame
        {
            PlayerId = 1,
            PlayerNumber = 0,
            StreamId = 5,
            Data = new byte[ProtocolConstants.MaxLiveFrameBytes + 1]
        }).Should().BeFalse();
    }

    [Fact]
    public void ProtocolCompatibility_ShouldHandleOldAndCurrentRanges()
    {
        var server = ProtocolProfile.ServerSupported;
        var exact = ProtocolCompat.Resolve(server, server);
        exact.Status.Should().Be(ProtocolCompatStatus.Exact);

        var oldMediaProtocol = new ProtocolRange(
            new ProtocolVer(2026, 4, 29, 1),
            new ProtocolVer(2026, 4, 29, 1));
        ProtocolCompat.Resolve(oldMediaProtocol, server).Status.Should().Be(ProtocolCompatStatus.ClientTooOld);

        var tooOld = new ProtocolRange(default, default);
        ProtocolCompat.Resolve(tooOld, server).Status.Should().Be(ProtocolCompatStatus.ClientTooOld);
    }

    [Fact]
    public void RoomPackets_ShouldValidateTypedBoundaries()
    {
        PacketValidation.IsValidRoomCreate(new PacketRoomCreate
        {
            RoomType = GameRoomType.PlayersRace,
            PlayersToStart = 4
        }).Should().BeTrue();

        PacketValidation.IsValidRoomCreate(new PacketRoomCreate
        {
            RoomType = (GameRoomType)255,
            PlayersToStart = 4
        }).Should().BeFalse();

        PacketValidation.IsValidRoomSetLaps(new PacketRoomSetLaps { Laps = 0 }).Should().BeFalse();
        PacketValidation.IsValidRoomRaceControl(new PacketRoomRaceControl { Action = RoomRaceControlAction.Pause }).Should().BeTrue();
        PacketValidation.IsValidRoomRaceControl(new PacketRoomRaceControl { Action = RoomRaceControlAction.None }).Should().BeFalse();
        PacketValidation.IsValidRoomPlayerReady(new PacketRoomPlayerReady { Car = (CarType)255 }).Should().BeFalse();
    }

    [Fact]
    public void MediaTransferLifecycle_ShouldUseExplicitStates()
    {
        ((byte)MediaTransferState.Idle).Should().Be(0);
        MediaTransferState.Receiving.Should().NotBe(MediaTransferState.Complete);
        MediaTransferState.Cancelled.Should().NotBe(MediaTransferState.Expired);
    }

    [Fact]
    public void ResumeIdentity_ShouldRequireServerIssuedToken()
    {
        var now = DateTime.UtcNow;
        ConnectionRecoveryRules.CanResume(
            ConnectionLifecycleState.Suspended,
            now,
            now,
            ConnectionRecoveryRules.DefaultReconnectGrace,
            playerId: 7,
            resumeToken: 11,
            requestedPlayerId: 7,
            requestedResumeToken: 11).Should().BeTrue();

        ConnectionRecoveryRules.CanResume(
            ConnectionLifecycleState.Suspended,
            now,
            now,
            ConnectionRecoveryRules.DefaultReconnectGrace,
            playerId: 7,
            resumeToken: 11,
            requestedPlayerId: 7,
            requestedResumeToken: 0).Should().BeFalse();
    }

    [Fact]
    public void StaleSequences_ShouldBeDetectedByMonotonicComparison()
    {
        PacketValidation.IsStaleSequence(currentSequence: 10, incomingSequence: 9).Should().BeTrue();
        PacketValidation.IsStaleSequence(currentSequence: 10, incomingSequence: 10).Should().BeTrue();
        PacketValidation.IsStaleSequence(currentSequence: 10, incomingSequence: 11).Should().BeFalse();
        PacketValidation.IsStaleSequence(currentSequence: 10, incomingSequence: 0).Should().BeFalse();
    }
}
