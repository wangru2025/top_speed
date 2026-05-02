namespace TopSpeed.Drive.Single
{
    internal sealed partial class SingleSession
    {
        public void Run(float elapsed)
        {
            RefreshCategoryVolumes();
            _session.Update(elapsed);
        }

        private void UpdatePositions()
        {
            _position = 1;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.PositionY > _car.PositionY)
                    _position++;
            }
        }

        private void TrackLocalCrashState()
        {
            var currentState = _car.State;
            var wasCrashing = _lastRecordedCarState == Vehicles.CarState.Crashing || _lastRecordedCarState == Vehicles.CarState.Crashed;
            var isCrashing = currentState == Vehicles.CarState.Crashing || currentState == Vehicles.CarState.Crashed;
            if (!wasCrashing && isCrashing)
                _localCrashCount++;

            _lastRecordedCarState = currentState;
        }

        private void ApplyPlayerFinishState()
        {
            _finished = true;
            TopSpeed.Drive.Session.FinishVehicle.Apply(_car, _finishLockController);
            _raceTime = _session.Context.ProgressMilliseconds;
            _requirePostFinishStopBeforeExit = true;
        }

        private void HandlePlayerNumberRequest()
        {
            if (_input.GetPlayerNumber())
                QueueSound(GetNumberSound(_playerNumber + 1));
        }

        private uint NextLocalMediaId()
        {
            _nextMediaId++;
            if (_nextMediaId == 0)
                _nextMediaId = 1;
            return _nextMediaId;
        }
    }
}
