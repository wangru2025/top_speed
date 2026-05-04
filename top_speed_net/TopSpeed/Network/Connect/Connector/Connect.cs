using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerConnector
    {
        public Task<ConnectResult> ConnectAsync(string host, int port, string callSign, TimeSpan timeout, CancellationToken token)
        {
            return ConnectAsync(host, port, callSign, timeout, token, resumePlayerId: 0, resumeToken: 0);
        }

        public async Task<ConnectResult> ConnectAsync(string host, int port, string callSign, TimeSpan timeout, CancellationToken token, uint resumePlayerId, ulong resumeToken)
        {
            if (string.IsNullOrWhiteSpace(host))
                return ConnectResult.CreateFail(LocalizationService.Mark("No server address was provided."));

            var resolve = await Task.Run(() => TryResolveHost(host), token).ConfigureAwait(false);
            if (!resolve.Success)
                return ConnectResult.CreateFail(resolve.Error);

            var address = resolve.Address!;
            if (port <= 0 || port > 65535)
                port = ClientProtocol.DefaultServerPort;

            var sanitizedCallSign = SanitizeCallSign(callSign);
            var endpoint = new IPEndPoint(address, port);

            var incoming = new ConcurrentQueue<IncomingPacket>();
            var listener = new EventBasedNetListener();
            NetPeer? connectedPeer = null;
            var disconnected = false;
            var disconnectReason = string.Empty;
            var sawProtocolPacketVersionMismatch = false;
            var remotePacketVersion = (byte)0;

            listener.PeerConnectedEvent += peer => connectedPeer = peer;
            listener.PeerDisconnectedEvent += (_, info) =>
            {
                disconnected = true;
                disconnectReason = info.Reason.ToString();
                incoming.Enqueue(new IncomingPacket(
                    Command.Disconnect,
                    new[] { ProtocolConstants.Version, (byte)Command.Disconnect },
                    DateTime.UtcNow.Ticks));
            };
            listener.NetworkReceiveEvent += (_, reader, _, _) =>
            {
                var data = reader.GetRemainingBytes();
                reader.Recycle();
                if (data.Length > 0 && data[0] != ProtocolConstants.Version)
                {
                    sawProtocolPacketVersionMismatch = true;
                    remotePacketVersion = data[0];
                    return;
                }

                if (ClientPacketSerializer.TryReadHeader(data, out var command))
                    incoming.Enqueue(new IncomingPacket(command, data, DateTime.UtcNow.Ticks));
            };

            var manager = new NetManager(listener)
            {
                UpdateTime = 1,
                ChannelsCount = PacketStreams.Count
            };

            if (!manager.Start())
                return ConnectResult.CreateFail(LocalizationService.Mark("Failed to initialize network client."));

            manager.Connect(endpoint.Address.ToString(), endpoint.Port, ProtocolConstants.ConnectionKey);

            var protocolHello = BuildProtocolHelloPacket(resumePlayerId, resumeToken);
            var hello = BuildPlayerHelloPacket(sanitizedCallSign);
            var keepAlive = new[] { ProtocolConstants.Version, (byte)Command.KeepAlive };
            var protocolHelloSent = false;
            var protocolNegotiated = false;
            var nextKeepAliveUtc = DateTime.UtcNow;
            byte? playerNumber = null;
            uint? playerId = null;
            string? motd = null;
            PacketProtocolWelcome? protocolWelcome = null;
            string? protocolFailureMessage = null;
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline && !token.IsCancellationRequested)
            {
                manager.PollEvents();

                if (disconnected && connectedPeer == null)
                {
                    manager.Stop();
                    return ConnectResult.CreateFail(LocalizationService.Format(
                        LocalizationService.Mark("Connection failed: {0}"),
                        disconnectReason));
                }

                if (!protocolHelloSent && connectedPeer != null && connectedPeer.ConnectionState == ConnectionState.Connected)
                {
                    var sendHelloResult = TrySendHandshakePacket(connectedPeer, protocolHello);
                    if (!sendHelloResult.Success)
                    {
                        manager.Stop();
                        return ConnectResult.CreateFail(sendHelloResult.Error);
                    }

                    protocolHelloSent = true;
                    nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                if (protocolHelloSent && connectedPeer != null && DateTime.UtcNow >= nextKeepAliveUtc)
                {
                    TrySendKeepAlive(connectedPeer, keepAlive);
                    nextKeepAliveUtc = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                }

                var poll = ProcessConnectPoll(
                    incoming,
                    manager,
                    connectedPeer,
                    hello,
                    protocolNegotiated,
                    playerId,
                    playerNumber,
                    motd,
                    protocolWelcome,
                    protocolFailureMessage,
                    disconnectReason,
                    sanitizedCallSign,
                    endpoint);

                protocolNegotiated = poll.ProtocolNegotiated;
                playerId = poll.PlayerId;
                playerNumber = poll.PlayerNumber;
                motd = poll.Motd;
                protocolWelcome = poll.ProtocolWelcome;
                protocolFailureMessage = poll.ProtocolFailureMessage;

                if (poll.Result.HasValue)
                    return poll.Result.Value;

                await Task.Delay(10, token).ConfigureAwait(false);
            }

            manager.Stop();
            if (token.IsCancellationRequested)
                return ConnectResult.CreateFail(LocalizationService.Mark("Connection attempt canceled."));

            if (protocolHelloSent && !protocolNegotiated)
            {
                if (sawProtocolPacketVersionMismatch)
                {
                    return ConnectResult.CreateFail(LocalizationService.Format(
                        LocalizationService.Mark("Protocol packet version mismatch. Server uses packet version {0}, client expects {1}. Update your client or server."),
                        remotePacketVersion,
                        ProtocolConstants.Version));
                }

                return ConnectResult.CreateFail(LocalizationService.Mark("No protocol negotiation response from server. The server may be outdated or incompatible."));
            }

            return ConnectResult.CreateFail(LocalizationService.Mark("No response from server. The server may be offline or unreachable."));
        }
    }
}

