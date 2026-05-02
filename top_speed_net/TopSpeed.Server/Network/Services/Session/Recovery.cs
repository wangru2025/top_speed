using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session
        {
            public PlayerConnection? ResolveResume(PlayerConnection pending, uint resumePlayerId, ulong resumeToken)
            {
                if (resumePlayerId == 0)
                {
                    if (_owner._players.Count > _owner._config.MaxPlayers)
                    {
                        RemoveConnection(
                            pending,
                            notifyRoom: false,
                            sendDisconnectPacket: true,
                            reason: "server_full",
                            disconnectMessage: LocalizationService.Mark("This server is full."));
                        return null;
                    }

                    pending.MarkConnected();
                    return pending;
                }

                if (!_owner._players.TryGetValue(resumePlayerId, out var existing))
                    return RejectResume(pending, "resume_unknown_player");

                if (!ConnectionRecoveryRules.CanResume(
                        existing.LifecycleState,
                        existing.SuspendedUtc,
                        DateTime.UtcNow,
                        ReconnectGrace,
                        existing.Id,
                        existing.ResumeToken,
                        resumePlayerId,
                        resumeToken))
                {
                    return RejectResume(pending, "resume_rejected");
                }

                _owner._endpointIndex.Remove(pending.EndPoint.ToString());
                _owner._players.Remove(pending.Id);
                existing.MarkReconnecting();
                existing.Rebind(pending.EndPoint);
                existing.Handshake = HandshakeState.Pending;
                existing.ClientVersion = pending.ClientVersion;
                existing.ClientSupportedRange = pending.ClientSupportedRange;
                existing.NegotiatedProtocol = pending.NegotiatedProtocol;
                _owner._endpointIndex[existing.EndPoint.ToString()] = existing.Id;

                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection resumed: playerId={0}, endpoint={1}."),
                    existing.Id,
                    existing.EndPoint));
                return existing;
            }

            public void SendInitialConnectionState(PlayerConnection player)
            {
                _owner.SendStream(player, PacketSerializer.WritePlayerNumber(player.Id, 0), PacketStream.Control);
                if (!string.IsNullOrWhiteSpace(_owner._config.Motd))
                    _owner.SendStream(player, PacketSerializer.WriteServerInfo(new PacketServerInfo { Motd = _owner._config.Motd }), PacketStream.Control);

                if (player.RoomId.HasValue && _owner._rooms.TryGetValue(player.RoomId.Value, out var room))
                {
                    if (room.PlayerIds.Contains(player.Id))
                    {
                        _owner.SendTrack(room, player);
                        _owner.SyncMediaTo(room, player);
                        _owner.SyncLiveTo(room, player);
                    }
                    else
                    {
                        _owner._room.JoinPlayer(player, room);
                    }

                    SendResumeRaceState(player, room);
                    _owner._notify.SendRoomState(player, room);
                }
                else
                {
                    _owner._room.HandleStateRequest(player);
                }

                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Connection established: playerId={0}, endpoint={1}, protocol={2}."),
                    player.Id,
                    player.EndPoint,
                    player.NegotiatedProtocol));
            }

            private PlayerConnection? RejectResume(PlayerConnection pending, string reason)
            {
                RemoveConnection(
                    pending,
                    notifyRoom: false,
                    sendDisconnectPacket: true,
                    reason: reason,
                    disconnectMessage: LocalizationService.Mark("Unable to resume the previous multiplayer session."));
                return null;
            }

            private void SendResumeRaceState(PlayerConnection player, RaceRoom room)
            {
                if (room == null)
                    return;

                if (room.RaceState == RoomRaceState.Racing && room.ActiveRaceParticipantIds.Contains(player.Id))
                {
                    _owner.SendStream(player, PacketSerializer.WriteGeneral(Command.StartRace), PacketStream.RaceEvent);
                    var payload = _owner.BuildRaceSnapshotPayload(room);
                    if (payload != null)
                        _owner.SendStream(player, payload, PacketStream.RaceState, PacketDeliveryKind.ReliableOrdered);
                    _owner._notify.ReplayRoomEventsTo(player, room, afterSequence: 0);
                }
                else if (room.RaceState == RoomRaceState.Completed)
                {
                    _owner._notify.ReplayRoomEventsTo(player, room, afterSequence: 0);
                    _owner._notify.RaceCompletedTo(player, room);
                }
                else if (room.RaceState == RoomRaceState.Aborted)
                {
                    _owner._notify.ReplayRoomEventsTo(player, room, afterSequence: 0);
                }
                else
                {
                    _owner.SendStream(player, PacketSerializer.WriteRoomRaceStateChanged(new PacketRoomRaceStateChanged
                    {
                        RoomId = room.Id,
                        RoomVersion = room.Version,
                        EventSequence = room.EventSequence,
                        RaceInstanceId = room.RaceInstanceId,
                        State = room.RaceState
                    }), PacketStream.Room);
                }
            }
        }
    }
}
