using System;
using System.Net;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Server.Config;
using TopSpeed.Server.Logging;
using TopSpeed.Server.Network;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ServerVoiceBehaviorTests
{
    [Fact]
    public void VoiceRelay_ShouldAcceptMismatchedPlayerNumber_WhenPlayerIdMatchesConnection()
    {
        using var fixture = new VoiceFixture();
        var host = fixture.Connect(new IPEndPoint(IPAddress.Loopback, 30201), "host");
        var guest = fixture.Connect(new IPEndPoint(IPAddress.Loopback, 30202), "guest");

        fixture.Send(host.EndPoint, ClientPacketSerializer.WriteRoomCreate("voice-room", GameRoomType.PlayersRace, 2));
        var hostSnapshot = fixture.Server.GetPlayerSnapshotForTest(host.PlayerId);
        var roomId = hostSnapshot.RoomId.GetValueOrDefault();
        roomId.Should().BeGreaterThan(0u);

        fixture.Send(guest.EndPoint, ClientPacketSerializer.WriteRoomJoin(roomId));
        var guestSnapshot = fixture.Server.GetPlayerSnapshotForTest(guest.PlayerId);
        guestSnapshot.RoomId.Should().Be(roomId);

        var wrongNumber = guestSnapshot.PlayerNumber == 0 ? (byte)1 : (byte)0;
        var streamId = 42u;
        fixture.Send(
            guest.EndPoint,
            ClientPacketSerializer.WritePlayerVoiceStart(
                guest.PlayerId,
                wrongNumber,
                streamId,
                LiveCodec.Opus,
                ProtocolConstants.VoiceSampleRate,
                1,
                ProtocolConstants.VoiceFrameMs,
                10,
                pushToTalk: true));

        var started = fixture.Server.GetPlayerSnapshotForTest(guest.PlayerId);
        started.HasVoice.Should().BeTrue();
        started.VoiceStreamId.Should().Be(streamId);
        started.VoiceFrequencyTenths.Should().Be(10);
        started.VoicePushToTalk.Should().BeTrue();

        fixture.Send(
            guest.EndPoint,
            ClientPacketSerializer.WritePlayerVoiceStop(
                guest.PlayerId,
                wrongNumber,
                streamId));

        var stopped = fixture.Server.GetPlayerSnapshotForTest(guest.PlayerId);
        stopped.HasVoice.Should().BeFalse();
    }

    private sealed class VoiceFixture : IDisposable
    {
        public VoiceFixture()
        {
            Logger = new Logger(LogLevel.None, logFilePath: null, writeToConsole: false);
            Server = new RaceServer(new RaceServerConfig { MaxPlayers = 8 }, Logger);
        }

        public RaceServer Server { get; }
        private Logger Logger { get; }
        private uint _nextPlayerId = 1;

        public VoiceClient Connect(IPEndPoint endPoint, string name)
        {
            var playerId = _nextPlayerId++;
            Send(endPoint, ClientPacketSerializer.WriteProtocolHello(new PacketProtocolHello
            {
                ClientVersion = ProtocolProfile.Current,
                MinSupported = ProtocolProfile.ClientSupported.MinSupported,
                MaxSupported = ProtocolProfile.ClientSupported.MaxSupported,
                ResumePlayerId = 0,
                ResumeToken = 0
            }));
            Send(endPoint, WritePlayerHello(name));
            return new VoiceClient(playerId, endPoint);
        }

        public void Send(IPEndPoint endPoint, byte[] payload)
        {
            Server.InjectPacketForTest(endPoint, payload);
        }

        public void Dispose()
        {
            Server.Dispose();
            Logger.Dispose();
        }

        private static byte[] WritePlayerHello(string name)
        {
            var buffer = new byte[2 + ProtocolConstants.MaxPlayerNameLength];
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerHello);
            writer.WriteFixedString(name ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }
    }

    private readonly struct VoiceClient
    {
        public VoiceClient(uint playerId, IPEndPoint endPoint)
        {
            PlayerId = playerId;
            EndPoint = endPoint;
        }

        public uint PlayerId { get; }
        public IPEndPoint EndPoint { get; }
    }
}
