using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void Create(PlayerConnection player, PacketRoomCreate packet)
            {
                var roomId = _owner.AllocateRoomId();
                var roomName = (packet.RoomName ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(roomName))
                    roomName = LocalizationService.Format(LocalizationService.Mark("Game {0}"), roomId);
                if (roomName.Length > ProtocolConstants.MaxRoomNameLength)
                    roomName = roomName.Substring(0, ProtocolConstants.MaxRoomNameLength);

                var roomType = RoomRules.NormalizeType(packet.RoomType);
                var playersToStart = RoomRules.NormalizePlayersToStart(roomType, packet.PlayersToStart);

                var room = new RaceRoom(roomId, roomName, roomType, playersToStart);
                _owner._rooms[room.Id] = room;
                SetTrackData(room, room.TrackName);
                JoinPlayer(player, room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomCreated);
                _owner._notify.ProtocolToLobby(LocalizationService.Format(
                    LocalizationService.Mark("{0} created game room {1}."),
                    RaceServer.DescribePlayer(player),
                    room.Name));
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Room created: room={0} \"{1}\", host={2}, type={3}, playersToStart={4}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    room.RoomType,
                    room.PlayersToStart));
            }

            public void Join(PlayerConnection player, PacketRoomJoin packet)
            {
                if (!_owner._rooms.TryGetValue(packet.RoomId, out var room))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.RoomNotFound, LocalizationService.Mark("Game room not found."));
                    return;
                }

                if (player.RoomId == room.Id && room.PlayerIds.Contains(player.Id))
                {
                    _owner._notify.SendRoomState(player, room);
                    return;
                }

                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._joinDeniedRaceInProgress++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Join denied: player={0}, room={1}, raceStarted={2}, preparing={3}."),
                        player.Id,
                        room.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("This game room is currently in progress."));
                    return;
                }

                if (GetRoomParticipantCount(room) >= room.PlayersToStart)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.RoomFull, RoomTexts.RoomUnavailableFull);
                    return;
                }

                JoinPlayer(player, room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Player joined room: room={0} \"{1}\", player={2}, participants={3}/{4}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    GetRoomParticipantCount(room),
                    room.PlayersToStart));
            }

            public void Leave(PlayerConnection player, bool notify)
            {
                if (!player.RoomId.HasValue)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                    return;
                }

                var roomId = player.RoomId.Value;
                _owner._trackPackageUploads.Remove(player.Id);
                if (!_owner._rooms.TryGetValue(roomId, out var room))
                {
                    player.RoomId = null;
                    player.Live = null;
                    if (player.LifecycleState != ConnectionLifecycleState.Closed
                        && player.LifecycleState != ConnectionLifecycleState.Expired)
                    {
                        player.MarkSessionReady();
                    }
                    _owner._notify.SendRoomState(player, null);
                    return;
                }

                var oldNumber = player.PlayerNumber;
                var leftName = RaceServer.DescribePlayer(player);
                room.PlayerIds.Remove(player.Id);
                _owner.RemoveRoomMemberPresence(room, player.Id);
                if (room.RaceStarted)
                {
                    _owner._race.MarkParticipantDnf(room, player.Id, oldNumber);
                }

                var previousHostId = room.HostId;
                player.RoomId = null;
                player.PlayerNumber = 0;
                player.State = PlayerState.NotReady;
                if (player.LifecycleState != ConnectionLifecycleState.Closed
                    && player.LifecycleState != ConnectionLifecycleState.Expired)
                {
                    player.MarkSessionReady();
                }
                room.PendingLoadouts.Remove(player.Id);
                room.PrepareSkips.Remove(player.Id);
                room.TrackReadyPlayers.Remove(player.Id);
                room.MediaMap.Remove(player.Id);
                _owner.StopLive(player, room, notifyRoom: notify);
                player.IncomingMedia = null;
                player.MediaLoaded = false;
                player.MediaPlaying = false;
                player.MediaId = 0;
                player.RadioVolumePercent = 100;

                if (notify)
                {
                    var disconnectPayload = PacketSerializer.WritePlayer(Command.PlayerDisconnected, player.Id, oldNumber);
                    _owner._notify.ToRoom(room, disconnectPayload, PacketStream.RaceEvent);
                    _owner._notify.ToRoom(room, disconnectPayload, PacketStream.Room);
                    _owner._notify.ProtocolToRoom(
                        room,
                        LocalizationService.Format(
                            LocalizationService.Mark("{0} has left the game."),
                            leftName));
                }

                _owner._notify.SendRoomState(player, null);

                var roomClosed = room.PlayerIds.Count == 0;
                if (roomClosed)
                {
                    _owner._rooms.Remove(room.Id);
                }
                else
                {
                    if (room.HostId == player.Id)
                        room.HostId = room.PlayerIds.OrderBy(id => id).First();
                    if (room.RaceStarted)
                        _owner._race.UpdateStopState(room);
                    if (room.PreparingRace)
                        _owner._race.TryStartAfterLoadout(room);
                    CompactNumbers(room);
                    TouchVersion(room);
                    _owner._notify.RoomParticipant(room, RoomEventKind.ParticipantLeft, player.Id, oldNumber, PlayerState.NotReady, leftName);
                    if (previousHostId != room.HostId)
                    {
                        if (_owner._players.TryGetValue(room.HostId, out var newHost))
                            _owner.SendProtocolMessage(newHost, ProtocolMessageCode.Ok, LocalizationService.Mark("You are now host of this room."));
                        _owner._notify.RoomLifecycle(room, RoomEventKind.HostChanged);
                    }
                    _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                    _owner._notify.BroadcastRoomState(room);
                }

                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Player left room: room={0} \"{1}\", player={2}, notify={3}, roomClosed={4}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    notify,
                    roomClosed));
                if (roomClosed)
                {
                    _owner._logger.Info(LocalizationService.Format(
                        LocalizationService.Mark("Room closed: room={0} \"{1}\", reason=last_participant_left."),
                        room.Id,
                        room.Name));
                }
            }

            public void JoinPlayer(PlayerConnection player, RaceRoom room)
            {
                if (player.RoomId == room.Id && room.PlayerIds.Contains(player.Id))
                {
                    _owner.SetRoomMemberPresence(room, player.Id, RoomMemberPresenceState.Active);
                    player.MarkInRoom();
                    _owner.SendStream(player, PacketSerializer.WritePlayerNumber(player.Id, player.PlayerNumber), PacketStream.Control);
                    _owner.SendTrack(room, player);
                    _owner.SyncMediaTo(room, player);
                    _owner.SyncLiveTo(room, player);
                    _owner._notify.SendRoomState(player, room);
                    return;
                }

                if (player.RoomId.HasValue)
                    Leave(player, true);

                room.PlayerIds.Add(player.Id);
                _owner.SetRoomMemberPresence(room, player.Id, RoomMemberPresenceState.Active);
                if (room.HostId == 0 || !room.PlayerIds.Contains(room.HostId))
                    room.HostId = player.Id;

                player.RoomId = room.Id;
                player.MarkInRoom();
                player.PlayerNumber = byte.MaxValue;
                player.State = PlayerState.NotReady;
                room.PrepareSkips.Remove(player.Id);
                room.TrackReadyPlayers.Remove(player.Id);
                CompactNumbers(room);

                _owner.SendStream(player, PacketSerializer.WritePlayerNumber(player.Id, player.PlayerNumber), PacketStream.Control);
                _owner.SendTrack(room, player);
                _owner.SyncMediaTo(room, player);
                _owner.SyncLiveTo(room, player);
                TouchVersion(room);
                _owner._notify.SendRoomState(player, room);
                _owner._notify.RoomParticipant(
                    room,
                    RoomEventKind.ParticipantJoined,
                    player.Id,
                    player.PlayerNumber,
                    player.State,
                    string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name);

                var joinedName = string.IsNullOrWhiteSpace(player.Name)
                    ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                    : player.Name;
                var joined = new PacketPlayerJoined { PlayerId = player.Id, PlayerNumber = player.PlayerNumber, Name = joinedName };
                _owner._notify.ToRoomExcept(room, player.Id, PacketSerializer.WritePlayerJoined(joined), PacketStream.Room);
                _owner._notify.BroadcastRoomState(room);
            }
        }
    }
}
