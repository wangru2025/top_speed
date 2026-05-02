namespace TopSpeed.Vehicles
{
    internal sealed partial class ComputerPlayer
    {
        public void StopAtFinish()
        {
            _finished = true;
            _state = ComputerState.Stopping;
            _remoteTargetSpeed = 0f;
            _remoteTargetX = _positionX;
            _remoteTargetY = _positionY;
            _horning = false;
            _soundHorn?.Stop();
            _soundBrake?.Stop();
            _soundBackfire?.Stop();
        }

        public void MarkFinished(float finishY)
        {
            StopAtFinish();
            if (finishY > 0f && _positionY < finishY)
                _positionY = finishY;
            _remoteTargetY = _positionY;
        }
    }
}
