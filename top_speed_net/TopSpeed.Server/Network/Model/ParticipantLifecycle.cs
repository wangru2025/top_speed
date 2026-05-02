namespace TopSpeed.Server.Network
{
    internal enum RaceParticipantLifecycleState : byte
    {
        Joined = 0,
        Preparing = 1,
        Racing = 2,
        Finished = 3,
        Dnf = 4,
        DisconnectedGrace = 5,
        Expired = 6,
        Aborted = 7
    }
}
