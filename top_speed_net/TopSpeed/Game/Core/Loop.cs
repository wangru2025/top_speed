namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public void Update(float deltaSeconds)
        {
            _input.Update();
            UpdateDriveTouchControls(deltaSeconds);
            UpdateMultiplayerMenuTouchControls();
            _driveInput.Run(_input.CaptureDriveInputFrame(), deltaSeconds);

            TryShowDeviceChoiceDialog();

            _driveInput.SetOverlayInputBlocked(
                _state == AppState.MultiplayerRace &&
                (_multiplayerCoordinator.Questions.HasActiveOverlayQuestion
                 || _dialogs.HasActiveOverlayDialog
                 || _choices.HasActiveChoiceDialog));

            HandleGlobalVolumeShortcuts();
            UpdateTextInputPrompt();
            UpdateSessionReconnect();
            _stateMachine.Update(deltaSeconds);
            _multiplayerCommunicatorRuntime.Update(deltaSeconds);

            if (_pendingDriveStart)
            {
                _pendingDriveStart = false;
                StartDrive(_pendingMode);
            }
        }
    }
}


