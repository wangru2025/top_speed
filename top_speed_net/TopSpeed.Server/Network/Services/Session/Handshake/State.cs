namespace TopSpeed.Server.Network
{
    internal enum HandshakeState
    {
        Pending = 0,
        AwaitingPlayerHello = 1,
        Complete = 2,
        Rejected = 3
    }
}
