using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
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
                var heartbeatMissed = _owner._players.Values
                    .Where(p => p.Connected
                                && p.Handshake == HandshakeState.Complete
                                && now - p.LastHeartbeatUtc > HeartbeatMissWindow)
                    .Select(p => p.Id)
                    .ToList();

                foreach (var id in heartbeatMissed)
                {
                    if (!_owner._players.TryGetValue(id, out var player))
                        continue;

                    SuspendConnection(player, sendDisconnectPacket: false, reason: "heartbeat_missed");
                }

                var expiredPreAuth = _owner._players.Values
                    .Where(p => p.Connected
                                && p.Handshake != HandshakeState.Complete
                                && now - p.LastHeartbeatUtc > ConnectTimeout)
                    .Select(p => p.Id)
                    .ToList();

                foreach (var id in expiredPreAuth)
                {
                    if (!_owner._players.TryGetValue(id, out var player))
                        continue;

                    RemoveConnection(player, notifyRoom: true, sendDisconnectPacket: true, reason: "connect_timeout");
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

            public void HandlePeerDisconnected(
                IPEndPoint endpoint,
                uint endpointEpoch,
                TransportDisconnectClassification disconnectClassification,
                DisconnectReason transportDisconnectReason,
                SocketError transportSocketError)
            {
                var key = endpoint.ToString();
                if (endpointEpoch != 0)
                {
                    if (!_owner._endpointEpochIndex.TryGetValue(key, out var expectedEpoch)
                        || expectedEpoch != endpointEpoch)
                    {
                        _owner._epochRejectCount++;
                        return;
                    }
                }

                if (!_owner._endpointIndex.TryGetValue(key, out var id))
                    return;
                if (!_owner._players.TryGetValue(id, out var player))
                    return;

                player.SetDisconnectOutcome(disconnectClassification.Reason, disconnectClassification.State);

                if (player.Handshake == HandshakeState.Complete)
                {
                    if (disconnectClassification.State == MultiplayerConnectionState.DisconnectedCleanly)
                    {
                        RemoveConnection(
                            player,
                            notifyRoom: true,
                            sendDisconnectPacket: false,
                            reason: disconnectClassification.SessionReasonCode);
                    }
                    else if (disconnectClassification.State == MultiplayerConnectionState.ProtocolError)
                    {
                        RemoveConnection(
                            player,
                            notifyRoom: true,
                            sendDisconnectPacket: false,
                            reason: disconnectClassification.SessionReasonCode);
                    }
                    else
                    {
                        SuspendConnection(
                            player,
                            sendDisconnectPacket: false,
                            reason: disconnectClassification.SessionReasonCode,
                            disconnectReason: disconnectClassification.Reason,
                            connectionState: disconnectClassification.State);
                    }
                }
                else
                {
                    RemoveConnection(
                        player,
                        notifyRoom: true,
                        sendDisconnectPacket: false,
                        reason: disconnectClassification.SessionReasonCode);
                }

                _owner._logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Transport disconnect observed: player={0}, endpoint={1}, transportReason={2}, socketError={3}, mappedReason={4}, mappedState={5}."),
                    player.Id,
                    endpoint,
                    transportDisconnectReason,
                    transportSocketError,
                    disconnectClassification.Reason,
                    disconnectClassification.State));
            }

            private void SuspendConnection(
                PlayerConnection player,
                bool sendDisconnectPacket,
                string reason,
                MultiplayerDisconnectReason disconnectReason = MultiplayerDisconnectReason.TimedOut,
                MultiplayerConnectionState connectionState = MultiplayerConnectionState.ConnectionLostSuspected)
            {
                if (player == null)
                    return;
                if (ConnectionRecoveryRules.IsRecoverableState(player.LifecycleState))
                    return;

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
                    _owner._trackPackageUploads.Remove(player.Id);
                    room.PendingLoadouts.Remove(player.Id);
                    room.PrepareSkips.Remove(player.Id);
                    room.TrackReadyPlayers.Remove(player.Id);
                    _owner.SetRoomMemberPresence(room, player.Id, RoomMemberPresenceState.Suspended);
                    var payload = PacketSerializer.WritePlayer(Command.PlayerDisconnected, player.Id, player.PlayerNumber);
                    _owner._notify.ToRoomExcept(room, player.Id, payload, PacketStream.RaceEvent);
                    _owner._notify.ToRoomExcept(room, player.Id, payload, PacketStream.Room);
                    _owner._notify.ProtocolToRoomExcept(
                        room,
                        player.Id,
                        LocalizationService.Format(
                            LocalizationService.Mark("{0} lost connection. Waiting for reconnect."),
                            RaceServer.DescribePlayer(player)));
                    _owner._notify.BroadcastRoomState(room);
                }

                _owner._endpointIndex.Remove(player.EndPoint.ToString());
                _owner._endpointEpochIndex.TryRemove(player.EndPoint.ToString(), out _);
                player.MarkSuspended();
                player.SetDisconnectOutcome(disconnectReason, connectionState);
                _owner._heartbeatSuspicionCount++;
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
                var resolvedOutcome = ResolveDisconnectOutcome(reason);
                player.SetDisconnectOutcome(resolvedOutcome.Reason, resolvedOutcome.State);
                var disconnectReason = player.LastDisconnectReason;
                var disconnectState = player.GameConnectionState;
                player.MarkClosed();
                if (player.RoomId.HasValue)
                    _owner._room.Leave(player, notifyRoom);
                _owner._trackPackageUploads.Remove(player.Id);
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
                _owner._endpointEpochIndex.TryRemove(player.EndPoint.ToString(), out _);
                _owner._players.Remove(player.Id);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection removed: player={0}, endpoint={1}, room={2}, reason={3}, mappedReason={4}, mappedState={5}."),
                    player.Id,
                    player.EndPoint,
                    roomId?.ToString() ?? LocalizationService.Translate(LocalizationService.Mark("none")),
                    reason,
                    disconnectReason,
                    disconnectState));
            }
        }
    }
}
