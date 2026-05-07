using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session
        {
            public static (MultiplayerDisconnectReason Reason, MultiplayerConnectionState State) ResolveDisconnectOutcome(string reason)
            {
                if (string.IsNullOrWhiteSpace(reason))
                    return (MultiplayerDisconnectReason.Unknown, MultiplayerConnectionState.DisconnectedCleanly);

                switch (reason)
                {
                    case "connect_timeout":
                    case "heartbeat_missed":
                    case "reconnect_grace_expired":
                        return (MultiplayerDisconnectReason.TimedOut, MultiplayerConnectionState.TimedOut);
                    case "reconnect_ip_mismatch":
                        return (MultiplayerDisconnectReason.ConnectionFailed, MultiplayerConnectionState.ConnectionLostSuspected);
                    case "reconnect_token_invalid":
                        return (MultiplayerDisconnectReason.ConnectionRejected, MultiplayerConnectionState.ProtocolError);
                    case "protocol_mismatch":
                    case "protocol_rejected":
                    case "protocol_error":
                        return (MultiplayerDisconnectReason.ProtocolError, MultiplayerConnectionState.ProtocolError);
                    case "server_full":
                    case "name_too_long":
                    case "name_repeated_letters":
                    case "name_duplicate":
                        return (MultiplayerDisconnectReason.Kicked, MultiplayerConnectionState.Kicked);
                    case "host_shutdown":
                        return (MultiplayerDisconnectReason.ServerShutdown, MultiplayerConnectionState.ServerShutdown);
                    case "peer_disconnect":
                        return (MultiplayerDisconnectReason.DisconnectedCleanly, MultiplayerConnectionState.DisconnectedCleanly);
                    default:
                        return (MultiplayerDisconnectReason.Unknown, MultiplayerConnectionState.ConnectionLostSuspected);
                }
            }

            public static string BuildDisconnectMessage(string reason)
            {
                if (string.IsNullOrWhiteSpace(reason))
                    return LocalizationService.Mark("The server closed the connection.");

                switch (reason)
                {
                    case "connect_timeout":
                        return LocalizationService.Mark("Connection setup timed out before authentication.");
                    case "heartbeat_missed":
                        return LocalizationService.Mark("Connection lost. Reconnect is available for a short time.");
                    case "reconnect_ip_mismatch":
                        return LocalizationService.Mark("Reconnect denied because the remote IP does not match the original session.");
                    case "reconnect_token_invalid":
                        return LocalizationService.Mark("Reconnect denied because the resume token is invalid.");
                    case "reconnect_grace_expired":
                        return LocalizationService.Mark("Reconnect window expired.");
                    case "protocol_mismatch":
                        return LocalizationService.Mark("Connection refused due to protocol mismatch.");
                    case "protocol_rejected":
                        return LocalizationService.Mark("Connection refused due to invalid protocol negotiation.");
                    case "protocol_error":
                        return LocalizationService.Mark("Connection closed due to protocol error.");
                    case "server_full":
                        return LocalizationService.Mark("This server is full.");
                    case "name_too_long":
                        return LocalizationService.Mark("Your call sign is too long for this server.");
                    case "name_repeated_letters":
                        return LocalizationService.Mark("Your call sign was rejected by moderation policy.");
                    case "name_duplicate":
                        return LocalizationService.Mark("This call sign is already in use on this server.");
                    case "host_shutdown":
                        return LocalizationService.Mark("The server will be shut down immediately by the host.");
                    case "peer_disconnect":
                        return LocalizationService.Mark("Connection closed.");
                    case "transport_connection_failed":
                    case "transport_network_unreachable":
                    case "transport_host_unreachable":
                    case "transport_peer_not_found":
                    case "transport_reconnect":
                    case "transport_unknown":
                        return LocalizationService.Mark("Connection lost. Reconnect is available for a short time.");
                    case "transport_connection_rejected":
                    case "transport_peer_to_peer_connection":
                        return LocalizationService.Mark("Connection rejected by transport policy.");
                    case "transport_unknown_host":
                        return LocalizationService.Mark("Unable to resolve server host.");
                    default:
                        return LocalizationService.Format(LocalizationService.Mark("Connection closed by server. Reason: {0}."), reason);
                }
            }
        }
    }
}
