using System;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Race
        {
            public void UpdateCompletions()
            {
                foreach (var room in _owner._rooms.Values)
                {
                    if (!room.RaceStarted)
                        continue;

                    UpdateStopState(room);
                }
            }

            public void UpdateStopState(GameRoom room)
            {
                if (room == null || room.RaceState != RoomRaceState.Racing)
                    return;

                if (RaceServer.GetRaceDistance(room) <= 0f)
                {
                    Abort(room, RoomRaceAbortReason.InvalidTrack);
                    return;
                }

                if (room.ActiveRaceParticipantIds.Count == 0)
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Race completion ready: room={0}, no active participants remain."),
                        room.Id));
                    Stop(room);
                    return;
                }

                var allTerminal = true;
                foreach (var id in room.ActiveRaceParticipantIds)
                {
                    var result = GetOrCreateParticipantResult(room, id);
                    if (RaceResultRules.IsTerminal(result.Status))
                        continue;

                    allTerminal = false;
                    break;
                }

                if (!allTerminal)
                    return;

                _owner._logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Race completion ready: room={0}, all active participants finished."),
                    room.Id));
                Stop(room);
            }

            public int CaptureFinishTimeMs(GameRoom room)
            {
                if (room.RaceStartedUtc == default(DateTime))
                    return 0;

                var elapsed = DateTime.UtcNow - room.RaceStartedUtc;
                if (elapsed <= TimeSpan.Zero)
                    return 0;

                var millis = (int)Math.Round(elapsed.TotalMilliseconds);
                return Math.Max(0, millis);
            }

            public bool TryMarkParticipantFinished(GameRoom room, uint playerId, byte playerNumber, int finishTimeMs, out byte finishOrder)
            {
                if (!RaceParticipantFinisher.TryMarkFinished(room, playerId, playerNumber, finishTimeMs, out finishOrder))
                {
                    if (room != null)
                    {
                        _owner._logger.Debug(LocalizationService.Format(
                            LocalizationService.Mark("Duplicate finish ignored: room={0}, player={1}, number={2}."),
                            room.Id,
                            playerId,
                            playerNumber));
                    }
                    return false;
                }
                return true;
            }

            public void MarkParticipantDnf(GameRoom room, uint playerId, byte playerNumber)
            {
                if (room == null)
                    return;
                if (!room.RaceParticipantResults.TryGetValue(playerId, out var result))
                {
                    result = new RoomRaceParticipantResult
                    {
                        PlayerId = playerId,
                        PlayerNumber = playerNumber,
                        Status = RoomRaceResultStatus.Pending,
                        Lifecycle = RaceParticipantLifecycleState.Racing
                    };
                    room.RaceParticipantResults[playerId] = result;
                }
                RaceParticipantFinisher.TryMarkDnf(room, playerId, playerNumber, RaceParticipantLifecycleState.Dnf);
            }

            public void FinalizeUnresolvedParticipantsAsDnf(GameRoom room)
            {
                foreach (var id in room.ActiveRaceParticipantIds)
                {
                    GetOrCreateParticipantResult(room, id);
                }

                foreach (var result in room.RaceParticipantResults.Values)
                {
                    var nextStatus = RaceResultRules.NormalizeCompletionStatus(result.Status);
                    if (nextStatus != result.Status)
                    {
                        result.Status = nextStatus;
                        if (result.Lifecycle != RaceParticipantLifecycleState.Finished)
                            result.Lifecycle = RaceParticipantLifecycleState.Dnf;
                        result.TimeMs = 0;
                        result.FinishOrder = 0;
                    }
                }
            }

            public void InitializeParticipants(GameRoom room)
            {
                room.RaceParticipantResults.Clear();
                room.RaceResults.Clear();
                room.RaceFinishTimesMs.Clear();

                foreach (var id in room.ActiveRaceParticipantIds)
                {
                    GetOrCreateParticipantResult(room, id);
                }
            }

            private RoomRaceParticipantResult GetOrCreateParticipantResult(GameRoom room, uint participantId)
            {
                if (!room.RaceParticipantResults.TryGetValue(participantId, out var result))
                {
                    result = new RoomRaceParticipantResult
                    {
                        PlayerId = participantId,
                        PlayerNumber = ResolveParticipantNumber(room, participantId),
                        Status = RoomRaceResultStatus.Pending,
                        Lifecycle = RaceParticipantLifecycleState.Racing
                    };
                    room.RaceParticipantResults[participantId] = result;
                }
                else if (result.PlayerNumber == 0)
                {
                    result.PlayerNumber = ResolveParticipantNumber(room, participantId);
                }

                return result;
            }

            private byte ResolveParticipantNumber(GameRoom room, uint participantId)
            {
                if (_owner._players.TryGetValue(participantId, out var player))
                    return player.PlayerNumber;

                if (TryGetActiveBot(room, participantId, out var bot))
                    return bot.PlayerNumber;

                if (room.RaceParticipantResults.TryGetValue(participantId, out var result))
                    return result.PlayerNumber;

                return 0;
            }

            private static bool TryGetActiveBot(GameRoom room, uint botId, out RoomBot bot)
            {
                for (var i = 0; i < room.Bots.Count; i++)
                {
                    var candidate = room.Bots[i];
                    if (candidate.Id != botId)
                        continue;
                    bot = candidate;
                    return true;
                }

                bot = null!;
                return false;
            }
        }
    }
}

