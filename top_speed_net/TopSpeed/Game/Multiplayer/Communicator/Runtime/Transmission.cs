using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private bool StartTransmission(Network.MultiplayerSession session, ushort frequencyTenths, bool pushToTalk)
        {
            var streamId = NextStreamId();
            if (!session.SendVoiceStart(streamId, _encoder.Profile, frequencyTenths, pushToTalk))
                return false;

            _encoder.Reset();
            _transmitting = true;
            _streamId = streamId;
            _activeFrequencyTenths = frequencyTenths;
            _activePushToTalk = pushToTalk;
            _timestampMs = 0;
            return true;
        }

        private void SendCapturedFrames(Network.MultiplayerSession session)
        {
            while (TryReadCapturedFrame(_captureFrame))
            {
                if (!_encoder.TryEncode(_captureFrame, _timestampMs, out var frame))
                    continue;

                if (!session.SendVoiceFrame(_streamId, in frame))
                {
                    StopTransmission();
                    return;
                }

                _timestampMs = unchecked(_timestampMs + (uint)ProtocolConstants.VoiceFrameMs);
            }
        }

        private void StopTransmission()
        {
            if (!_transmitting)
                return;

            _boundSession?.SendVoiceStop(_streamId);
            _transmitting = false;
            _streamId = 0;
            _activeFrequencyTenths = 0;
            _activePushToTalk = false;
            _timestampMs = 0;
            _encoder.Reset();
        }
    }
}
