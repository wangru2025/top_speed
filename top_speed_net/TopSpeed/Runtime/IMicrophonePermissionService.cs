namespace TopSpeed.Runtime
{
    public interface IMicrophonePermissionService
    {
        bool IsMicrophonePermissionGranted();
        bool EnsureMicrophonePermissionGranted();
    }
}
