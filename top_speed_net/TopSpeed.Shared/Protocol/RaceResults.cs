namespace TopSpeed.Protocol
{
    public static class RaceResultRules
    {
        public static bool IsTerminal(RoomRaceResultStatus status)
        {
            return status == RoomRaceResultStatus.Finished || status == RoomRaceResultStatus.Dnf;
        }

        public static RoomRaceResultStatus NormalizeCompletionStatus(RoomRaceResultStatus status)
        {
            return IsTerminal(status) ? status : RoomRaceResultStatus.Dnf;
        }

        public static RoomRaceResultStatus ResolveParticipantStatus(RoomRaceResultStatus current, RoomRaceResultStatus requested)
        {
            if (current == RoomRaceResultStatus.Finished)
                return RoomRaceResultStatus.Finished;
            if (requested == RoomRaceResultStatus.Finished)
                return RoomRaceResultStatus.Finished;
            if (requested == RoomRaceResultStatus.Dnf)
                return RoomRaceResultStatus.Dnf;
            return current == RoomRaceResultStatus.None ? RoomRaceResultStatus.Pending : current;
        }

        public static bool ShouldComplete(RoomRaceResultStatus[] statuses)
        {
            if (statuses == null || statuses.Length == 0)
                return true;

            for (var i = 0; i < statuses.Length; i++)
            {
                if (!IsTerminal(statuses[i]))
                    return false;
            }

            return true;
        }
    }
}
