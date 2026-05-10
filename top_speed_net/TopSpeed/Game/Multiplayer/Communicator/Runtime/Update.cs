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

            var pttHeld = _input.IsDown(InputKey.V);
            var shouldTransmit = ShouldTransmit(pttHeld, out var pushToTalk);
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
        // VOX gates on the recent-voice-activity hold window (anything quieter than
        // VoiceActivationThreshold for VoiceActivationHoldMs stops transmission).
        // PTT gates strictly on the V key being held.
        private bool ShouldTransmit(bool pttHeld, out bool pushToTalk)
        {
            if (_multiplayer.CommunicatorVoiceActivationEnabled)
            {
                pushToTalk = false;
                return IsVoiceActivityActive();
            }

            pushToTalk = true;
            return pttHeld;
        }

        private bool IsVoiceActivityActive()
        {
            var last = _lastVoiceActivityUtcTicks;
            if (last <= 0)
                return false;

            var holdTicks = TimeSpan.FromMilliseconds(VoiceActivationHoldMs).Ticks;
            return DateTime.UtcNow.Ticks - last <= holdTicks;
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
