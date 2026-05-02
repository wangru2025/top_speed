using System;
using System.Linq;
using System.Net;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session
        {
            public void CleanupExpiredConnections()
            {
                var now = DateTime.UtcNow;
                var expiredConnected = _owner._players.Values
                    .Where(p => p.Connected && now - p.LastSeenUtc > ConnectionTimeout)
                    .Select(p => p.Id)
                    .ToList();

                foreach (var id in expiredConnected)
                {
                    if (!_owner._players.TryGetValue(id, out var player))
                        continue;

                    if (player.RoomId.HasValue && player.Handshake == HandshakeState.Complete)
                        SuspendConnection(player, sendDisconnectPacket: false, reason: "timeout");
                    else
                        RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: true, reason: "timeout");
                }

                var expiredDisconnected = _owner._players.Values
                    .Where(p => ConnectionRecoveryRules.IsGraceExpired(p.LifecycleState, p.SuspendedUtc, now, ReconnectGrace))
                    .Select(p => p.Id)
                    .ToList();

                foreach (var id in expiredDisconnected)
                {
                    if (_owner._players.TryGetValue(id, out var player))
                    {
                        player.MarkExpired();
                        RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: false, reason: "reconnect_grace_expired");
                    }
                }

                _owner.CleanupLiveStreams();
            }

            public void HandlePeerDisconnected(IPEndPoint endpoint)
            {
                lock (_owner._lock)
                {
                    var key = endpoint.ToString();
                    if (!_owner._endpointIndex.TryGetValue(key, out var id))
                        return;
                    if (!_owner._players.TryGetValue(id, out var player))
                        return;

                    if (player.RoomId.HasValue && player.Handshake == HandshakeState.Complete)
                        SuspendConnection(player, sendDisconnectPacket: false, reason: "peer_disconnect");
                    else
                        RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: false, reason: "peer_disconnect");
                }
            }

            private void SuspendConnection(PlayerConnection player, bool sendDisconnectPacket, string reason)
            {
                var roomId = player.RoomId;
                RaceRoom? room = null;
                if (roomId.HasValue)
                    _owner._rooms.TryGetValue(roomId.Value, out room);

                if (sendDisconnectPacket)
                    _owner.SendStream(player, PacketSerializer.WriteDisconnect(BuildDisconnectMessage(reason)), PacketStream.Control);

                if (room != null)
                {
                    _owner.StopLive(player, room, notifyRoom: true);
                    _owner.ResetMediaState(player, room);
                    var payload = PacketSerializer.WritePlayer(Command.PlayerDisconnected, player.Id, player.PlayerNumber);
                    _owner._notify.ToRoomExcept(room, player.Id, payload, PacketStream.RaceEvent);
                    _owner._notify.ToRoomExcept(room, player.Id, payload, PacketStream.Room);
                    _owner._notify.ProtocolToRoomExcept(
                        room,
                        player.Id,
                        LocalizationService.Format(
                            LocalizationService.Mark("{0} lost connection. Waiting for reconnect."),
                            RaceServer.DescribePlayer(player)));
                }

                _owner._endpointIndex.Remove(player.EndPoint.ToString());
                player.MarkSuspended();
                if (room != null && room.HostId == player.Id)
                    MigrateHostAfterSuspend(room, player.Id);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection suspended for reconnect: player={0}, endpoint={1}, room={2}, reason={3}."),
                    player.Id,
                    player.EndPoint,
                    roomId?.ToString() ?? LocalizationService.Translate(LocalizationService.Mark("none")),
                    reason));
            }

            private void MigrateHostAfterSuspend(RaceRoom room, uint suspendedHostId)
            {
                foreach (var id in room.PlayerIds.OrderBy(id => id))
                {
                    if (id == suspendedHostId)
                        continue;
                    if (!_owner._players.TryGetValue(id, out var candidate))
                        continue;
                    if (!candidate.Connected)
                        continue;

                    room.HostId = candidate.Id;
                    _owner._room.TouchVersion(room);
                    _owner.SendProtocolMessage(candidate, ProtocolMessageCode.Ok, LocalizationService.Mark("You are now host of this room."));
                    _owner._notify.RoomLifecycle(room, RoomEventKind.HostChanged);
                    _owner._notify.BroadcastRoomState(room);
                    return;
                }
            }

            public void RemoveConnection(
                PlayerConnection player,
                bool notifyRoom,
                bool sendDisconnectPacket,
                string reason,
                string? disconnectMessage = null,
                bool announcePresenceDisconnect = true)
            {
                var roomId = player.RoomId;
                player.MarkClosed();
                if (player.RoomId.HasValue)
                    _owner._room.Leave(player, notifyRoom);
                if (announcePresenceDisconnect && player.ServerPresenceAnnounced)
                    _owner.BroadcastServerDisconnectAnnouncement(player, reason);
                if (sendDisconnectPacket)
                {
                    var message = string.IsNullOrWhiteSpace(disconnectMessage)
                        ? BuildDisconnectMessage(reason)
                        : disconnectMessage;
                    _owner.SendStream(player, PacketSerializer.WriteDisconnect(message), PacketStream.Control);
                }

                _owner._endpointIndex.Remove(player.EndPoint.ToString());
                _owner._players.Remove(player.Id);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection removed: player={0}, endpoint={1}, room={2}, reason={3}."),
                    player.Id,
                    player.EndPoint,
                    roomId?.ToString() ?? LocalizationService.Translate(LocalizationService.Mark("none")),
                    reason));
            }
        }
    }
}
