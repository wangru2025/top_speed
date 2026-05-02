namespace TS.Audio
{
    internal enum AudioLifecycleState
    {
        Active,
        Stopping,
        QueuedForDispose,
        Disposing,
        Disposed
    }
}
