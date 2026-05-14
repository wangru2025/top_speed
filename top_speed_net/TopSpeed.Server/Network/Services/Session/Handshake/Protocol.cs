using System;
using System.Net;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private bool HandlePendingHandshake(PlayerConnection player, Command command, byte[] payload, IPEndPoint endPoint)
        {
            if (player.Handshake == HandshakeState.Complete)
                return false;
            if (player.Handshake == HandshakeState.Rejected)
                return true;

            if (command == Command.KeepAlive || command == Command.Ping || command == Command.ClientHeartbeat)
            {
                player.LastHeartbeatUtc = DateTime.UtcNow;
                return true;
            }

            if (player.Handshake == HandshakeState.AwaitingPlayerHello)
            {
                if (command != Command.PlayerHello)
                {
                    _logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Ignoring pre-identification command from {0}: {1}."),
                        player.EndPoint,
                        command));
                    return true;
                }

                if (!PacketSerializer.TryReadPlayerHello(payload, out var playerHello))
                {
                    RejectHandshake(player, LocalizationService.Mark("Invalid player identification packet."));
                    PacketFail(endPoint, Command.PlayerHello);
                    return true;
                }

                player.LastHeartbeatUtc = DateTime.UtcNow;
                HandlePlayerHello(player, playerHello);
                return true;
            }

            if (command != Command.ProtocolHello)
            {
                _logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Ignoring pre-protocol command from {0}: {1}."),
                    player.EndPoint,
                    command));
                return true;
            }

            if (!PacketSerializer.TryReadProtocolHello(payload, out var hello))
            {
                RejectHandshake(player, LocalizationService.Mark("Invalid protocol handshake packet."));
                PacketFail(endPoint, Command.ProtocolHello);
                return true;
            }

            player.LastHeartbeatUtc = DateTime.UtcNow;
            EvaluateProtocolHello(player, hello);
            return true;
        }

        private void EvaluateProtocolHello(PlayerConnection player, PacketProtocolHello hello)
        {
            ProtocolRange clientRange;
            try
            {
                clientRange = new ProtocolRange(hello.MinSupported, hello.MaxSupported);
            }
            catch (ArgumentException)
            {
                RejectHandshake(player, LocalizationService.Mark("Invalid protocol range in handshake packet."));
                return;
            }

            player.ClientVersion = hello.ClientVersion;
            player.ClientSupportedRange = clientRange;
            if (hello.ClientVersion < clientRange.MinSupported || hello.ClientVersion > clientRange.MaxSupported)
            {
                RejectHandshake(
                    player,
                    LocalizationService.Format(
                        LocalizationService.Mark("Invalid protocol handshake. Reported protocol version {0} is outside declared supported range {1}."),
                        hello.ClientVersion,
                        clientRange));
                return;
            }

            var serverRange = ProtocolProfile.ServerSupported;
            var compat = ProtocolCompat.Resolve(clientRange, serverRange);
            var effectiveStatus = ResolveEffectiveCompatibilityStatusForTest(compat, hello.ClientVersion);

            if (!compat.IsCompatible)
            {
                var rejectedWelcome = new PacketProtocolWelcome
                {
                    Status = effectiveStatus,
                    NegotiatedVersion = compat.NegotiatedVersion,
                    ServerMinSupported = serverRange.MinSupported,
                    ServerMaxSupported = serverRange.MaxSupported,
                    Message = BuildHandshakeMessage(effectiveStatus, hello.ClientVersion, ProtocolProfile.Current, serverRange)
                };
                SendStream(player, PacketSerializer.WriteProtocolWelcome(rejectedWelcome), PacketStream.Control);
                _logger.Warning(
                    LocalizationService.Format(
                        LocalizationService.Mark("Protocol rejected: player={0}, endpoint={1}, reportedProtocolVersion={2}, clientSupported={3}, serverSupported={4}, status={5}."),
                        player.Id,
                        player.EndPoint,
                        hello.ClientVersion,
                        clientRange,
                        serverRange,
                        effectiveStatus));
                player.Handshake = HandshakeState.Rejected;
                RemoveConnection(
                    player,
                    notifyRoom: false,
                    sendDisconnectPacket: true,
                    reason: "protocol_mismatch",
                    disconnectMessage: rejectedWelcome.Message);
                return;
            }

            var resolvedPlayer = _session.ResolveResume(player, hello.ResumePlayerId, hello.ResumeToken);
            if (resolvedPlayer == null)
                return;
            player = resolvedPlayer;
            player.Handshake = HandshakeState.AwaitingPlayerHello;
            var negotiatedProtocol = ResolveNegotiatedVersionForSessionForTest(
                effectiveStatus,
                compat.NegotiatedVersion,
                hello.ClientVersion);
            player.NegotiatedProtocol = negotiatedProtocol;
            player.MarkProtocolNegotiated();

            var welcome = new PacketProtocolWelcome
            {
                Status = effectiveStatus,
                NegotiatedVersion = negotiatedProtocol,
                ServerMinSupported = serverRange.MinSupported,
                ServerMaxSupported = serverRange.MaxSupported,
                ResumeToken = player.ResumeToken,
                Message = BuildHandshakeMessage(effectiveStatus, hello.ClientVersion, ProtocolProfile.Current, serverRange)
            };
            SendStream(player, PacketSerializer.WriteProtocolWelcome(welcome), PacketStream.Control);
        }

        internal static ProtocolCompatStatus ResolveEffectiveCompatibilityStatusForTest(ProtocolCompatResult compat, ProtocolVer clientVersion)
        {
            if (!compat.IsCompatible)
                return compat.Status;

            return clientVersion == ProtocolProfile.Current
                ? ProtocolCompatStatus.Exact
                : ProtocolCompatStatus.CompatibleDowngrade;
        }

        internal static ProtocolVer ResolveNegotiatedVersionForSessionForTest(
            ProtocolCompatStatus status,
            ProtocolVer negotiatedVersion,
            ProtocolVer clientVersion)
        {
            return status == ProtocolCompatStatus.Exact
                ? clientVersion
                : negotiatedVersion;
        }

        private void RejectHandshake(PlayerConnection player, string message)
        {
            var serverRange = ProtocolProfile.ServerSupported;
            var clientSupportedRange = player.ClientSupportedRange?.ToString()
                                       ?? LocalizationService.Translate(LocalizationService.Mark("unknown"));
            var welcome = new PacketProtocolWelcome
            {
                Status = ProtocolCompatStatus.NoCommonVersion,
                NegotiatedVersion = default,
                ServerMinSupported = serverRange.MinSupported,
                ServerMaxSupported = serverRange.MaxSupported,
                Message = message ?? LocalizationService.Mark("Protocol negotiation failed.")
            };
            _logger.Warning(
                LocalizationService.Format(
                    LocalizationService.Mark("Protocol rejected: player={0}, endpoint={1}, reportedProtocolVersion={2}, clientSupported={3}, serverSupported={4}, reason={5}"),
                    player.Id,
                    player.EndPoint,
                    player.ClientVersion,
                    clientSupportedRange,
                    serverRange,
                    welcome.Message));

            SendStream(player, PacketSerializer.WriteProtocolWelcome(welcome), PacketStream.Control);
            SendProtocolMessage(player, ProtocolMessageCode.Failed, message ?? LocalizationService.Mark("Connection refused due to protocol mismatch."));
            player.Handshake = HandshakeState.Rejected;
            RemoveConnection(
                player,
                notifyRoom: false,
                sendDisconnectPacket: true,
                reason: "protocol_rejected",
                disconnectMessage: welcome.Message);
        }

        private static string BuildHandshakeMessage(ProtocolCompatStatus status, ProtocolVer clientVersion, ProtocolVer serverCurrentVersion, ProtocolRange serverRange)
        {
            switch (status)
            {
                case ProtocolCompatStatus.Exact:
                    return LocalizationService.Mark("Protocol compatibility verified.");
                case ProtocolCompatStatus.CompatibleDowngrade:
                    if (clientVersion > serverCurrentVersion)
                        return LocalizationService.Format(
                            LocalizationService.Mark("Your client protocol version {0} is newer than this server protocol version {1}. This server supports protocol versions {2}. You can continue, but some features may behave differently or may not work at all."),
                            clientVersion,
                            serverCurrentVersion,
                            serverRange);

                    return LocalizationService.Format(
                        LocalizationService.Mark("Your client protocol version {0} is older than this server protocol version {1}. This server supports protocol versions {2}. You can continue, but some features may behave differently or may not work at all."),
                        clientVersion,
                        serverCurrentVersion,
                        serverRange);
                case ProtocolCompatStatus.ClientTooOld:
                    return LocalizationService.Format(
                        LocalizationService.Mark("Your client protocol version is out-of-date: {0}. This server supports protocol versions {1}. Please update your client."),
                        clientVersion,
                        serverRange);
                case ProtocolCompatStatus.ClientTooNew:
                    return LocalizationService.Format(
                        LocalizationService.Mark("Your client protocol version is too new: {0} and does not match this server protocol. This server supports protocol versions {1}. The server needs an update."),
                        clientVersion,
                        serverRange);
                default:
                    return LocalizationService.Format(
                        LocalizationService.Mark("Your client protocol version is {0}. This server supports protocol versions {1}."),
                        clientVersion,
                        serverRange);
            }
        }
    }
}


