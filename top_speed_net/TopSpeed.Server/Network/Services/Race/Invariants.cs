using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal static class RaceCompletionInvariants
    {
        public static bool TryValidateTerminalResults(GameRoom room, out string reason)
        {
            reason = string.Empty;
            if (room == null)
            {
                reason = "room_null";
                return false;
            }

            var finishOrders = new HashSet<byte>();
            foreach (var result in room.RaceParticipantResults.Values)
            {
                if (!RaceResultRules.IsTerminal(result.Status))
                {
                    reason = "unresolved_participant";
                    return false;
                }

                if (result.Status != RoomRaceResultStatus.Finished)
                    continue;

                if (result.FinishOrder == 0)
                {
                    reason = "finished_without_order";
                    return false;
                }

                if (!finishOrders.Add(result.FinishOrder))
                {
                    reason = "duplicate_finish_order";
                    return false;
                }
            }

            return true;
        }
    }
}

