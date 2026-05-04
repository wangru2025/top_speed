using System;
using System.Collections.Concurrent;
using System.Net;
using LiteNetLib;
using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerConnector
    {
        private static ConnectPollState ProcessConnectPoll(
            ConcurrentQueue<IncomingPacket> incoming,
            NetManager manager,
            NetPeer? connectedPeer,
            byte[] hello,
            bool protocolNegotiated,
            uint? playerId,
            byte? playerNumber,
            string? motd,
            PacketProtocolWelcome? protocolWelcome,
            string? protocolFailureMessage,
            string disconnectReason,
            string sanitizedCallSign,
            IPEndPoint endpoint)
        {
            var state = new ConnectPollState
            {
                ProtocolNegotiated = protocolNegotiated,
                PlayerId = playerId,
                PlayerNumber = playerNumber,
                Motd = motd,
                ProtocolWelcome = protocolWelcome,
                ProtocolFailureMessage = protocolFailureMessage
            };

            var disconnectedDuringPoll = false;
            while (incoming.TryDequeue(out var packet))
            {
                if (packet.Command == Command.Disconnect)
                {
                    if (ClientPacketSerializer.TryReadDisconnect(packet.Payload, out var disconnectMessage) &&
                        !string.IsNullOrWhiteSpace(disconnectMessage))
                    {
                        state.ProtocolFailureMessage = disconnectMessage;
                    }

                    disconnectedDuringPoll = true;
                    continue;
                }

                if (packet.Command == Command.PlayerNumber && ClientPacketSerializer.TryReadPlayer(packet.Payload, out var assigned))
                {
                    if (!state.ProtocolNegotiated)
                    {
                        manager.Stop();
                        state.Result = ConnectResult.CreateFail(LocalizationService.Mark("This server does not support required protocol negotiation. Please update your server."));
                        return state;
                    }

                    state.PlayerId = assigned.PlayerId;
                    state.PlayerNumber = assigned.PlayerNumber;
                    if (!string.IsNullOrWhiteSpace(state.Motd))
                    {
                        state.Result = ConnectResult.CreateSuccess(
                            manager,
                            connectedPeer,
                            endpoint,
                            assigned.PlayerId,
                            assigned.PlayerNumber,
                            state.Motd,
                            sanitizedCallSign,
                            incoming,
                            state.ProtocolWelcome);
                        return state;
                    }
                }
                else if (packet.Command == Command.ServerInfo && ClientPacketSerializer.TryReadServerInfo(packet.Payload, out var info))
                {
                    state.Motd = info.Motd;
                    if (state.PlayerId.HasValue && state.PlayerNumber.HasValue)
                    {
                        state.Result = ConnectResult.CreateSuccess(
                            manager,
                            connectedPeer,
                            endpoint,
                            state.PlayerId.Value,
                            state.PlayerNumber.Value,
                            state.Motd,
                            sanitizedCallSign,
                            incoming,
                            state.ProtocolWelcome);
                        return state;
                    }
                }
                else if (packet.Command == Command.ProtocolWelcome && ClientPacketSerializer.TryReadProtocolWelcome(packet.Payload, out var welcome))
                {
                    state.ProtocolWelcome = welcome;
                    var acceptedCompatibility = IsCompatibilityAccepted(welcome.Status)
                        && welcome.NegotiatedVersion == ProtocolProfile.Current;
                    if (!acceptedCompatibility)
                    {
                        state.ProtocolFailureMessage = ResolveProtocolCompatibilityFailure(welcome);
                        disconnectedDuringPoll = true;
                        continue;
                    }

                    if (!state.ProtocolNegotiated && connectedPeer != null)
                    {
                        var helloResult = TrySendHandshakePacket(connectedPeer, hello);
                        if (!helloResult.Success)
                        {
                            manager.Stop();
                            state.Result = ConnectResult.CreateFail(helloResult.Error);
                            return state;
                        }

                        state.ProtocolNegotiated = true;
                    }
                }
                else if (packet.Command == Command.ProtocolMessage && ClientPacketSerializer.TryReadProtocolMessage(packet.Payload, out var protocolMessage))
                {
                    if (protocolMessage.Code == ProtocolMessageCode.Failed)
                    {
                        state.ProtocolFailureMessage = string.IsNullOrWhiteSpace(protocolMessage.Message)
                            ? LocalizationService.Mark("Connection refused by server.")
                            : protocolMessage.Message;
                        disconnectedDuringPoll = true;
                        continue;
                    }
                }
            }

            if (disconnectedDuringPoll)
            {
                manager.Stop();
                state.Result = BuildDisconnectedResult(state.ProtocolFailureMessage, state.ProtocolWelcome, disconnectReason);
                return state;
            }

            if (state.ProtocolNegotiated && state.PlayerId.HasValue && state.PlayerNumber.HasValue && connectedPeer != null)
            {
                state.Result = ConnectResult.CreateSuccess(
                    manager,
                    connectedPeer,
                    endpoint,
                    state.PlayerId.Value,
                    state.PlayerNumber.Value,
                    state.Motd,
                    sanitizedCallSign,
                    incoming,
                    state.ProtocolWelcome);
            }

            return state;
        }

        private static ConnectResult BuildDisconnectedResult(
            string? protocolFailureMessage,
            PacketProtocolWelcome? protocolWelcome,
            string disconnectReason)
        {
            if (!string.IsNullOrWhiteSpace(protocolFailureMessage))
                return ConnectResult.CreateFail(protocolFailureMessage!);

            if (protocolWelcome != null && !IsCompatibilityAccepted(protocolWelcome.Status))
            {
                var fallback = BuildProtocolRefusalFallback(protocolWelcome);
                return ConnectResult.CreateFail(fallback);
            }

            var reason = string.IsNullOrWhiteSpace(disconnectReason)
                ? LocalizationService.Mark("The server refused the connection.")
                : LocalizationService.Format(
                    LocalizationService.Mark("The server refused the connection. Details: {0}."),
                    disconnectReason);
            return ConnectResult.CreateFail(reason);
        }

        private struct ConnectPollState
        {
            public bool ProtocolNegotiated;
            public uint? PlayerId;
            public byte? PlayerNumber;
            public string? Motd;
            public PacketProtocolWelcome? ProtocolWelcome;
            public string? ProtocolFailureMessage;
            public ConnectResult? Result;
        }
    }
}

