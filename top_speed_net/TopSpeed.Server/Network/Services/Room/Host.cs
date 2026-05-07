using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void SetTrack(PlayerConnection player, PacketRoomSetTrack packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room track change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change track while race setup or race is active."));
                    return;
                }

                if (packet.Track == null || !PacketValidation.IsValidTrackPackageRef(packet.Track))
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, LocalizationService.Mark("Invalid track selection."));
                    return;
                }

                if (packet.Track.IsBuiltIn)
                {
                    var trackName = (packet.Track.BuiltInTrackKey ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(trackName))
                    {
                        _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, LocalizationService.Mark("Track cannot be empty."));
                        return;
                    }

                    SetTrackData(room, trackName);
                }
                else
                {
                    if (!IsSelectionEnabled(room))
                    {
                        _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Custom tracks are not enabled for this room."));
                        return;
                    }

                    if (!SetTrackData(room, packet.Track))
                    {
                        _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidTrack, LocalizationService.Mark("Custom track package is not available on server."));
                        return;
                    }
                }

                _owner.SendTrackToRoom(room);
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.TrackChanged);
            }

            public void SetLaps(PlayerConnection player, PacketRoomSetLaps packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room laps change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change laps while race setup or race is active."));
                    return;
                }

                if (packet.Laps < 1 || packet.Laps > 16)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidLaps, LocalizationService.Mark("Laps must be between 1 and 16."));
                    return;
                }

                room.Laps = packet.Laps;
                if (room.TrackSelected)
                {
                    if (room.TrackSelection != null && room.TrackSelection.IsCustomPackage)
                    {
                        if (!SetTrackData(room, room.TrackSelection))
                            SetTrackData(room, "america");
                    }
                    else
                    {
                        SetTrackData(room, room.TrackName);
                    }
                }
                _owner.SendTrackToRoom(room);
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.LapsChanged);
            }

            public void StartRace(PlayerConnection player)
            {
                if (!TryGetHosted(player, out var room))
                    return;

                var minimumParticipants = _owner._race.GetMinimumParticipantsToStart(room);
                var activeParticipants = _owner.GetActiveParticipantCountForStartBarrier(room);
                if (activeParticipants < minimumParticipants)
                {
                    _owner._startBarrierBlockedInsufficientActive++;
                    _owner.SendProtocolMessage(
                        player,
                        ProtocolMessageCode.Failed,
                        LocalizationService.Format(
                            LocalizationService.Mark("Not enough players. {0} required to start."),
                            minimumParticipants));
                    return;
                }

                if (room.RaceStarted)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("A race is already in progress."));
                    return;
                }

                if (room.PreparingRace)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race setup is already in progress."));
                    return;
                }

                _owner._race.TransitionState(room, RoomRaceState.Preparing);
                room.PendingLoadouts.Clear();
                room.PrepareSkips.Clear();
                _owner.ResetRoomTrackReadiness(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.PrepareStarted);
                _owner._race.AssignRandomBotLoadouts(room);
                _owner._race.AnnounceBotsReady(room);
                _owner._logger.Info(LocalizationService.Format(
                    LocalizationService.Mark("Race prepare started: room={0} \"{1}\", requestedBy={2}, humans={3}, bots={4}, capacity={5}, minStart={6}."),
                    room.Id,
                    room.Name,
                    player.Id,
                    _owner.GetActiveHumanParticipantCount(room),
                    room.Bots.Count,
                    room.PlayersToStart,
                    minimumParticipants));

                _owner._notify.ProtocolToRoom(
                    room,
                    LocalizationService.Format(
                        LocalizationService.Mark("{0} is about to start the game. Choose your vehicle and transmission mode."),
                        RaceServer.DescribePlayer(player)));
                _owner._race.TryStartAfterLoadout(room);
            }

            public void SetPlayersToStart(PlayerConnection player, PacketRoomSetPlayersToStart packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room player-limit change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change player limit while race setup or race is active."));
                    return;
                }

                var value = packet.PlayersToStart;
                if (value < 2 || value > ProtocolConstants.MaxRoomPlayersToStart)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("Player limit must be between 2 and 10."));
                    return;
                }

                if (room.RoomType == GameRoomType.OneOnOne && value != 2)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("One-on-one rooms always allow a maximum of 2 players."));
                    return;
                }

                value = RoomRules.NormalizePlayersToStart(room.RoomType, value);
                if (GetRoomParticipantCount(room) > value)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.InvalidPlayersToStart, LocalizationService.Mark("Cannot set lower than current players in room."));
                    return;
                }

                room.PlayersToStart = value;
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.PlayersToStartChanged);
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
            }

            public void SetGameRules(PlayerConnection player, PacketRoomSetGameRules packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room game-rules change denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot change game rules while race setup or race is active."));
                    return;
                }

                var requestedFlags = packet.GameRulesFlags;
                if (!_owner._config.Features.CustomTracks
                    && (requestedFlags & (uint)RoomGameRules.CustomTracks) != 0u)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Custom tracks are disabled on this server."));
                    return;
                }

                var allowedFlags = (uint)RoomGameRules.GhostMode;
                if (_owner._config.Features.CustomTracks)
                    allowedFlags |= (uint)RoomGameRules.CustomTracks;
                var normalizedFlags = requestedFlags & allowedFlags;
                if (room.GameRulesFlags == normalizedFlags)
                    return;

                room.GameRulesFlags = normalizedFlags;
                TouchVersion(room);
                _owner._notify.RoomLifecycle(room, RoomEventKind.GameRulesChanged);
            }
        }
    }
}

