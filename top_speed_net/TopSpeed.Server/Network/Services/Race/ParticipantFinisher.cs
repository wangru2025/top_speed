using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal static class RaceParticipantFinisher
    {
        public static bool TryMarkFinished(GameRoom room, uint playerId, byte playerNumber, int finishTimeMs, out byte finishOrder)
        {
            finishOrder = 0;
            if (room == null || room.RaceState != RoomRaceState.Racing)
                return false;

            if (!room.RaceParticipantResults.TryGetValue(playerId, out var result))
            {
                result = new RoomRaceParticipantResult
                {
                    PlayerId = playerId,
                    PlayerNumber = playerNumber,
                    Status = RoomRaceResultStatus.Pending,
                    Lifecycle = RaceParticipantLifecycleState.Racing,
                    TimeMs = 0,
                    FinishOrder = 0
                };
                room.RaceParticipantResults[playerId] = result;
            }

            if (result.Status == RoomRaceResultStatus.Finished)
                return false;

            var order = 1;
            foreach (var entry in room.RaceParticipantResults.Values)
            {
                if (entry.Status == RoomRaceResultStatus.Finished)
                    order++;
            }

            result.PlayerNumber = playerNumber;
            result.Status = RaceResultRules.ResolveParticipantStatus(result.Status, RoomRaceResultStatus.Finished);
            result.Lifecycle = RaceParticipantLifecycleState.Finished;
            result.TimeMs = Math.Max(0, finishTimeMs);
            result.FinishOrder = (byte)Math.Min(order, byte.MaxValue);
            finishOrder = result.FinishOrder;

            if (!room.RaceResults.Contains(playerNumber))
                room.RaceResults.Add(playerNumber);

            room.RaceFinishTimesMs[playerNumber] = Math.Max(0, finishTimeMs);
            return true;
        }

        public static bool TryMarkDnf(
            GameRoom room,
            uint playerId,
            byte playerNumber,
            RaceParticipantLifecycleState lifecycle)
        {
            if (room == null)
                return false;

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

            if (result.Status == RoomRaceResultStatus.Finished)
                return false;

            result.PlayerNumber = playerNumber;
            result.Status = RaceResultRules.ResolveParticipantStatus(result.Status, RoomRaceResultStatus.Dnf);
            result.Lifecycle = lifecycle;
            result.TimeMs = 0;
            result.FinishOrder = 0;
            return true;
        }
    }
}

