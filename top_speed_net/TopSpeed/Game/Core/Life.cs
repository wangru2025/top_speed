namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public void Dispose()
        {
            _updateDownloadCts?.Cancel();
            _updateDownloadCts?.Dispose();
            _driveGyroscopeSensor?.Dispose();
            _driveGyroscopeSensor = null;
            _driveAccelerometerSensor?.Dispose();
            _driveAccelerometerSensor = null;
            _logo?.Dispose();
            _multiplayerCommunicatorRuntime.Dispose();
            _menu.Dispose();
            _input.Dispose();
            _session?.SetPacketSink(null);
            _session?.Dispose();
            _speech.Dispose();
            _audio.Dispose();
        }

        public void FadeOutMenuMusic(int durationMs = 1000)
        {
            _menu.FadeOutMenuMusic(durationMs);
        }
    }
}
