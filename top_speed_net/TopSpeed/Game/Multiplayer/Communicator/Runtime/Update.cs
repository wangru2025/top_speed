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

            if (session == null || !session.IsConnected)
            {
                ClearRemoteStreams();
                Disarm();
                return;
            }

            var localAudibleFrequencyTenths = _multiplayer.CommunicatorEnabled
                ? _multiplayer.CommunicatorFrequencyTenths
                : (ushort)0;
            UpdateRemoteAudibility(localAudibleFrequencyTenths);
            CleanupTimedOutRemoteStreams();

            if (!IsArmed())
            {
                Disarm();
                return;
            }

            // Arm: bring up the capture device the moment the communicator is enabled,
            // so VOX and PTT can transmit immediately instead of losing the first frames
            // to a cold-start of MiniAudio while the user is already talking.
            if (!EnsureCaptureInitialized())
            {
                Disarm();
                return;
            }

            var shouldTransmit = ShouldTransmit(out var pushToTalk);
            var frequencyTenths = _multiplayer.CommunicatorFrequencyTenths;

            if (_transmitting &&
                (!shouldTransmit
                 || _activeFrequencyTenths != frequencyTenths
                 || _activePushToTalk != pushToTalk))
            {
                EndTransmission();
            }

            if (shouldTransmit && !_transmitting)
            {
                if (!BeginTransmission(session, frequencyTenths, pushToTalk))
                    return;
            }

            if (_transmitting)
                SendCapturedFrames(session);
            else
                DiscardCapturedSamples();
        }

        private bool IsArmed()
        {
            return _multiplayer.CommunicatorEnabled
                && _multiplayer.CommunicatorFrequencyTenths != 0;
        }

        // Single source of truth for "should we be transmitting right now?".
        // VOX mode: continuous transmission while voice activation is enabled. No
        // voice-activity detector — the user explicitly opted into open-mic.
        // PTT mode: transmit only while the V key is held with no modifier keys
        // (Ctrl / Shift / Alt). The modifier exclusion is critical because
        // Ctrl+Shift+V is the toggle shortcut for VOX itself; without it, pressing
        // the toggle would briefly open PTT and play the activation cue.
        private bool ShouldTransmit(out bool pushToTalk)
        {
            if (_multiplayer.CommunicatorVoiceActivationEnabled)
            {
                pushToTalk = false;
                return true;
            }

            pushToTalk = true;
            return IsUnmodifiedKeyDown(InputKey.V);
        }

        private bool IsUnmodifiedKeyDown(InputKey key)
        {
            if (!_input.IsDown(key))
                return false;

            return !_input.IsDown(InputKey.LeftControl)
                && !_input.IsDown(InputKey.RightControl)
                && !_input.IsDown(InputKey.LeftShift)
                && !_input.IsDown(InputKey.RightShift)
                && !_input.IsDown(InputKey.LeftAlt)
                && !_input.IsDown(InputKey.RightAlt);
        }

        private uint NextStreamId()
        {
            var id = _nextStreamId++;
            if (id == 0)
                id = _nextStreamId++;

            return id;
        }
    }
}
