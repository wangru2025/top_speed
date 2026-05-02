namespace TS.Audio
{
    public enum AudioDiagnosticKind
    {
        OutputCreated,
        OutputBackendDiagnostics,
        OutputBackendStateChanged,
        OutputBackendAnomaly,
        OutputDisposed,
        OutputMasterVolumeChanged,
        OutputRoomAcousticsChanged,

        BusCreated,
        BusDisposed,
        BusVolumeChanged,
        BusMuteChanged,
        BusEffectsEnabledChanged,
        BusEffectAdded,
        BusEffectMoved,
        BusEffectRemoved,
        BusEffectsCleared,

        SourceCreated,
        SourceDisposed,
        SourceLifecycleChanged,
        SourceAudioThreadException,
        SourcePlayRequested,
        SourceStarted,
        SourceStopped,
        SourceFadeStarted,
        SourceLoopingChanged,
        SourceSeeked,
        SourceVolumeChanged,
        SourcePitchChanged,
        SourcePanChanged,
        SourceStereoWideningChanged,
        SourcePositionChanged,
        SourceVelocityChanged,
        SourceDistanceModelChanged,
        SourceRoomAcousticsChanged,
        SourceDopplerChanged,
        SourceEnded,

        StreamCreated,
        StreamDisposed,
        StreamPlayRequested,
        StreamStopped,

        AnomalyClippingRisk,
        AnomalySilentStart,
        AnomalySuspiciousSourceConfig
    }
}
