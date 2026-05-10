using System;
using TopSpeed.Input;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        public void Update(float _elapsed)
        {
            var session = _getSession();
            if (!ReferenceEquals(session, _boundSession))
                BindSession(session);

            var pttHeld = _input.IsDown(InputKey.V);
            UpdateLocalMicCueState(session, pttHeld);

            if (session == null || !session.IsConnected)
                ClearRemoteStreams();

            var frequencyTenths = _multiplayer.CommunicatorFrequencyTenths;
            var localAudibleFrequencyTenths = _multiplayer.CommunicatorEnabled ? frequencyTenths : (ushort)0;
            UpdateRemoteAudibility(localAudibleFrequencyTenths);
            CleanupTimedOutRemoteStreams();

            if (ShouldMonitorVoiceActivity(session) && !EnsureCaptureInitialized())
            {
                StopTransmission();
                ClearCapturedSamples();
                return;
            }

            if (!TryGetTransmitState(session, pttHeld, out var shouldTransmit, out var pushToTalk))
            {
                StopTransmission();
                ClearCapturedSamples();
                return;
            }

            if (!shouldTransmit)
            {
                StopTransmission();
                ClearCapturedSamples();
                return;
            }

            if (!EnsureCaptureInitialized())
            {
                StopTransmission();
                return;
            }

            if (_transmitting && (_activeFrequencyTenths != frequencyTenths || _activePushToTalk != pushToTalk))
                StopTransmission();

            if (!_transmitting)
            {
                if (!StartTransmission(session!, frequencyTenths, pushToTalk))
                    return;
            }

            SendCapturedFrames(session!);
        }

        private bool TryGetTransmitState(Network.MultiplayerSession? session, bool pttHeld, out bool shouldTransmit, out bool pushToTalk)
        {
            shouldTransmit = false;
            pushToTalk = false;

            if (session == null || !session.IsConnected)
                return false;
            if (!_multiplayer.CommunicatorEnabled)
                return false;
            if (_multiplayer.CommunicatorFrequencyTenths == 0)
                return false;

            if (_multiplayer.CommunicatorVoiceActivationEnabled)
            {
                pushToTalk = false;
                if (pttHeld)
                {
                    shouldTransmit = false;
                    return true;
                }

                shouldTransmit = true;
                return true;
            }

            pushToTalk = true;
            shouldTransmit = pttHeld;
            return true;
        }

        private uint NextStreamId()
        {
            var id = _nextStreamId++;
            if (id == 0)
                id = _nextStreamId++;

            return id;
        }

        private bool IsVoiceActivityActive()
        {
            var last = _lastVoiceActivityUtcTicks;
            if (last <= 0)
                return false;

            var holdTicks = TimeSpan.FromMilliseconds(VoiceActivationHoldMs).Ticks;
            return DateTime.UtcNow.Ticks - last <= holdTicks;
        }

        private bool ShouldMonitorVoiceActivity(Network.MultiplayerSession? session)
        {
            return session != null
                && session.IsConnected
                && _multiplayer.CommunicatorEnabled
                && _multiplayer.CommunicatorFrequencyTenths != 0
                && _multiplayer.CommunicatorVoiceActivationEnabled;
        }

        private void UpdateLocalMicCueState(Network.MultiplayerSession? session, bool pttHeld)
        {
            var shouldOpen = ShouldLocalMicCueBeOpen(session, pttHeld);
            if (_localMicCueOpen == shouldOpen)
                return;

            _localMicCueOpen = shouldOpen;
            OnLocalTransmissionStateChanged(shouldOpen);
        }

        private bool ShouldLocalMicCueBeOpen(Network.MultiplayerSession? session, bool pttHeld)
        {
            if (!_multiplayer.CommunicatorEnabled)
                return false;

            if (_multiplayer.CommunicatorVoiceActivationEnabled)
                return !pttHeld;

            return pttHeld;
        }
    }
}
