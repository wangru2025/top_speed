using LiteNetLib;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal readonly struct TransportDisconnectClassification
    {
        public TransportDisconnectClassification(
            MultiplayerDisconnectReason reason,
            MultiplayerConnectionState state,
            string sessionReasonCode)
        {
            Reason = reason;
            State = state;
            SessionReasonCode = sessionReasonCode;
        }

        public MultiplayerDisconnectReason Reason { get; }
        public MultiplayerConnectionState State { get; }
        public string SessionReasonCode { get; }
    }

    internal static class DisconnectMapping
    {
        public static TransportDisconnectClassification FromTransportReason(DisconnectReason reason)
        {
            return reason switch
            {
                DisconnectReason.Timeout => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.TimedOut,
                    MultiplayerConnectionState.TimedOut,
                    "heartbeat_missed"),
                DisconnectReason.RemoteConnectionClose => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.DisconnectedCleanly,
                    MultiplayerConnectionState.DisconnectedCleanly,
                    "peer_disconnect"),
                DisconnectReason.DisconnectPeerCalled => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.LocalDisconnect,
                    MultiplayerConnectionState.DisconnectedCleanly,
                    "peer_disconnect"),
                DisconnectReason.InvalidProtocol => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.ProtocolError,
                    MultiplayerConnectionState.ProtocolError,
                    "protocol_error"),
                DisconnectReason.ConnectionFailed => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.ConnectionFailed,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    "transport_connection_failed"),
                DisconnectReason.ConnectionRejected => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.ConnectionRejected,
                    MultiplayerConnectionState.ProtocolError,
                    "transport_connection_rejected"),
                DisconnectReason.NetworkUnreachable => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.NetworkError,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    "transport_network_unreachable"),
                DisconnectReason.HostUnreachable => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.HostUnreachable,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    "transport_host_unreachable"),
                DisconnectReason.UnknownHost => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.UnknownHost,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    "transport_unknown_host"),
                DisconnectReason.Reconnect => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.ReconnectRequested,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    "transport_reconnect"),
                DisconnectReason.PeerNotFound => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.PeerNotFound,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    "transport_peer_not_found"),
                DisconnectReason.PeerToPeerConnection => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.ProtocolError,
                    MultiplayerConnectionState.ProtocolError,
                    "transport_peer_to_peer_connection"),
                _ => new TransportDisconnectClassification(
                    MultiplayerDisconnectReason.Unknown,
                    MultiplayerConnectionState.ConnectionLostSuspected,
                    "transport_unknown")
            };
        }
    }
}
