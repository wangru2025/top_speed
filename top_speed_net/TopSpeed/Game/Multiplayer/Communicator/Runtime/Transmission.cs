using TopSpeed.Protocol;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime
    {
        private bool BeginTransmission(Network.MultiplayerSession session, ushort frequencyTenths, bool pushToTalk)
        {
            var streamId = NextStreamId();
            if (!session.SendVoiceStart(streamId, _encoder.Profile, frequencyTenths, pushToTalk))
            {
                VoiceDebug.Log($"tx: SendVoiceStart returned false stream={streamId} freq={frequencyTenths} ptt={pushToTalk}");
                return false;
            }

            _encoder.Reset();
            _transmitting = true;
            _streamId = streamId;
            _activeFrequencyTenths = frequencyTenths;
            _activePushToTalk = pushToTalk;
            _timestampMs = 0;
            _txFrameCount = 0;
            _txByteCount = 0;
            // Don't replay whatever ambient noise was captured while the runtime was
            // idle; this lets remote players hear the speaker from the first frame.
            DiscardCapturedSamples();
            UpdateMicCue(open: true);
            VoiceDebug.Log($"tx: BeginTransmission stream={streamId} freq={frequencyTenths} ptt={pushToTalk}");
            return true;
        }

        private void SendCapturedFrames(Network.MultiplayerSession session)
        {
            while (TryReadCapturedFrame(_captureFrame))
            {
                if (!_encoder.TryEncode(_captureFrame, _timestampMs, out var frame))
                {
                    // Encoder failures are isolated to a single 20 ms window; advancing the
                    // timestamp keeps the receiver's playout clock aligned so subsequent
                    // frames don't appear to be in the past.
                    _timestampMs = unchecked(_timestampMs + (uint)ProtocolConstants.VoiceFrameMs);
                    VoiceDebug.Log($"tx: encoder.TryEncode failed stream={_streamId} ts={_timestampMs}");
                    continue;
                }

                if (!session.SendVoiceFrame(_streamId, in frame))
                {
                    VoiceDebug.Log($"tx: SendVoiceFrame returned false stream={_streamId} ts={_timestampMs} payloadBytes={frame.Payload.Length}");
                    EndTransmission();
                    return;
                }

                _txFrameCount++;
                _txByteCount += frame.Payload.Length;
                if (_txFrameCount == 1)
                    VoiceDebug.Log($"tx: first frame sent stream={_streamId} payloadBytes={frame.Payload.Length}");
                else if (_txFrameCount % 50 == 0)
                    VoiceDebug.Log($"tx: progress stream={_streamId} frames={_txFrameCount} bytes={_txByteCount}");

                _timestampMs = unchecked(_timestampMs + (uint)ProtocolConstants.VoiceFrameMs);
            }
        }

        private void EndTransmission()
        {
            if (!_transmitting)
                return;

            var session = _boundSession;
            var streamId = _streamId;
            var frames = _txFrameCount;
            var bytes = _txByteCount;
            _transmitting = false;
            _streamId = 0;
            _activeFrequencyTenths = 0;
            _activePushToTalk = false;
            _timestampMs = 0;
            _txFrameCount = 0;
            _txByteCount = 0;
            _encoder.Reset();
            session?.SendVoiceStop(streamId);
            UpdateMicCue(open: false);
            VoiceDebug.Log($"tx: EndTransmission stream={streamId} totalFrames={frames} totalBytes={bytes}");
        }

        // "Disarm" tears the runtime back down to the disabled-communicator state:
        // stop transmitting, drop any captured samples, and release the mic so the
        // OS doesn't keep the capture device open when the user has turned the
        // communicator off (or the session has disconnected).
        private void Disarm()
        {
            EndTransmission();
            DiscardCapturedSamples();
            DisposeCapture();
        }
    }
}
