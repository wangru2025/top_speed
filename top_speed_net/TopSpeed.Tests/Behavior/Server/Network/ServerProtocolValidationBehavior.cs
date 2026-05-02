using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ServerProtocolValidationBehaviorTests
{
    [Fact]
    public void RoomCreateReader_ShouldRejectInvalidEnumValues()
    {
        var payload = new byte[2 + ProtocolConstants.MaxRoomNameLength + 1 + 1];
        var writer = new PacketWriter(payload);
        writer.WriteByte(ProtocolConstants.Version);
        writer.WriteByte((byte)Command.RoomCreate);
        writer.WriteFixedString("room", ProtocolConstants.MaxRoomNameLength);
        writer.WriteByte(255);
        writer.WriteByte(2);

        PacketSerializer.TryReadRoomCreate(payload, out _).Should().BeFalse();
    }

    [Fact]
    public void RoomReadyReader_ShouldRejectInvalidVehicleValues()
    {
        var payload = new byte[2 + 1 + 1];
        var writer = new PacketWriter(payload);
        writer.WriteByte(ProtocolConstants.Version);
        writer.WriteByte((byte)Command.RoomPlayerReady);
        writer.WriteByte(255);
        writer.WriteBool(true);

        PacketSerializer.TryReadRoomPlayerReady(payload, out _).Should().BeFalse();
    }

    [Fact]
    public void ProtocolHelloReader_ShouldAcceptOldHelloWithoutResumeAndNewHelloWithResume()
    {
        var oldHello = BuildProtocolHello(includeResumeId: false, includeResumeToken: false);
        PacketSerializer.TryReadProtocolHello(oldHello, out var oldPacket).Should().BeTrue();
        oldPacket.ResumePlayerId.Should().Be(0);
        oldPacket.ResumeToken.Should().Be(0);

        var newHello = BuildProtocolHello(includeResumeId: true, includeResumeToken: true);
        PacketSerializer.TryReadProtocolHello(newHello, out var newPacket).Should().BeTrue();
        newPacket.ResumePlayerId.Should().Be(42);
        newPacket.ResumeToken.Should().Be(99);
    }

    [Fact]
    public void MediaReader_ShouldRejectOldPacketWithoutTransferId()
    {
        var payload = new byte[2 + 4 + 1 + 4 + 4 + ProtocolConstants.MaxMediaFileExtensionLength];
        var writer = new PacketWriter(payload);
        writer.WriteByte(ProtocolConstants.Version);
        writer.WriteByte((byte)Command.PlayerMediaBegin);
        writer.WriteUInt32(1);
        writer.WriteByte(0);
        writer.WriteUInt32(10);
        writer.WriteUInt32(128);
        writer.WriteFixedString("ogg", ProtocolConstants.MaxMediaFileExtensionLength);

        PacketSerializer.TryReadPlayerMediaBegin(payload, out _).Should().BeFalse();
    }

    [Fact]
    public void ProtocolVersion_ShouldRejectPreTransferIdMediaProtocol()
    {
        var oldMediaProtocol = new ProtocolRange(
            new ProtocolVer(2026, 4, 29, 1),
            new ProtocolVer(2026, 4, 29, 1));

        ProtocolCompat.Resolve(oldMediaProtocol, ProtocolProfile.ServerSupported)
            .Status.Should().Be(ProtocolCompatStatus.ClientTooOld);
    }

    private static byte[] BuildProtocolHello(bool includeResumeId, bool includeResumeToken)
    {
        var payload = 5 + 5 + 5 + (includeResumeId ? 4 : 0) + (includeResumeToken ? 8 : 0);
        var buffer = new byte[2 + payload];
        var writer = new PacketWriter(buffer);
        writer.WriteByte(ProtocolConstants.Version);
        writer.WriteByte((byte)Command.ProtocolHello);
        WriteVersion(ref writer, ProtocolProfile.Current);
        WriteVersion(ref writer, ProtocolProfile.ClientSupported.MinSupported);
        WriteVersion(ref writer, ProtocolProfile.ClientSupported.MaxSupported);
        if (includeResumeId)
            writer.WriteUInt32(42);
        if (includeResumeToken)
            writer.WriteUInt64(99);
        return buffer;
    }

    private static void WriteVersion(ref PacketWriter writer, ProtocolVer version)
    {
        writer.WriteUInt16(version.Year);
        writer.WriteByte(version.Month);
        writer.WriteByte(version.Day);
        writer.WriteByte(version.Revision);
    }
}
