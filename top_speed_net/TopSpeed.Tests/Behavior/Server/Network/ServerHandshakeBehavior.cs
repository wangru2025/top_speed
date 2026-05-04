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
public sealed class ServerHandshakeBehaviorTests
{
    [Fact]
    public void Handshake_ShouldWaitForPlayerIdentificationBeforeCompleting()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello();

        var snapshot = fixture.Server.GetPlayerSnapshotForTest(1);
        snapshot.Handshake.Should().Be(HandshakeState.AwaitingPlayerHello);

        fixture.Send(ClientPacketSerializer.WriteGeneral(Command.Ping));
        fixture.Server.GetStressSnapshotForTest().PlayerCount.Should().Be(0);
    }

    [Fact]
    public void Handshake_ShouldRejectInvalidNameBeforeConnectionEstablishes()
    {
        using var fixture = new HandshakeFixture(new ServerModerationSettings
        {
            MaxNameLength = 4,
            BlockRepeatedLettersInName = false,
            AllowDuplicateNames = true
        });
        fixture.SendProtocolHello();
        fixture.SendPlayerHello("abcdef");

        fixture.Server.GetStressSnapshotForTest().PlayerCount.Should().Be(0);
        Action readRemovedPlayer = () => fixture.Server.GetPlayerSnapshotForTest(1);
        readRemovedPlayer.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Handshake_ShouldCompleteAfterValidPlayerIdentification()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello();
        fixture.SendPlayerHello("pilot");

        var snapshot = fixture.Server.GetPlayerSnapshotForTest(1);
        snapshot.Handshake.Should().Be(HandshakeState.Complete);
        snapshot.LifecycleState.Should().Be(ConnectionLifecycleState.Connected);
    }

    [Fact]
    public void Handshake_ShouldReject_WhenNegotiatedProtocolDiffersFromClientVersion()
    {
        using var fixture = new HandshakeFixture();
        fixture.SendProtocolHello(new PacketProtocolHello
        {
            ClientVersion = ProtocolProfile.ClientSupported.MinSupported,
            MinSupported = ProtocolProfile.ClientSupported.MinSupported,
            MaxSupported = ProtocolProfile.ClientSupported.MaxSupported,
            ResumePlayerId = 0,
            ResumeToken = 0
        });

        Action readRemovedPlayer = () => fixture.Server.GetPlayerSnapshotForTest(1);
        readRemovedPlayer.Should().Throw<InvalidOperationException>();
    }

    private sealed class HandshakeFixture : IDisposable
    {
        public HandshakeFixture(ServerModerationSettings? moderation = null)
        {
            Logger = new Logger(LogLevel.None, logFilePath: null, writeToConsole: false);
            Server = new RaceServer(new RaceServerConfig
            {
                MaxPlayers = 8,
                Moderation = moderation ?? new ServerModerationSettings()
            }, Logger);
        }

        public RaceServer Server { get; }
        private Logger Logger { get; }
        private IPEndPoint Endpoint { get; } = new(IPAddress.Loopback, 30101);

        public void Send(byte[] payload)
        {
            Server.InjectPacketForTest(Endpoint, payload);
        }

        public void SendProtocolHello()
        {
            SendProtocolHello(new PacketProtocolHello
            {
                ClientVersion = ProtocolProfile.Current,
                MinSupported = ProtocolProfile.ClientSupported.MinSupported,
                MaxSupported = ProtocolProfile.ClientSupported.MaxSupported,
                ResumePlayerId = 0,
                ResumeToken = 0
            });
        }

        public void SendProtocolHello(PacketProtocolHello hello)
        {
            Send(ClientPacketSerializer.WriteProtocolHello(hello));
        }

        public void SendPlayerHello(string name)
        {
            Send(WritePlayerHello(name));
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
}
