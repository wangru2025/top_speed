namespace TopSpeed.Vehicles.Live
{
    internal sealed partial class LiveRadio
    {
        private void OnRender(float[] buffer, int frames, int channels, ref ulong frameIndex)
        {
            if (buffer == null || frames <= 0 || channels <= 0)
                return;

            if (!System.Threading.Monitor.TryEnter(_lock))
            {
                System.Array.Clear(buffer, 0, buffer.Length);
                return;
            }

            try
            {
                var sampleCount = frames * channels;
                if (sampleCount <= 0 || sampleCount > buffer.Length)
                    return;

                var cursor = 0;
                for (var frame = 0; frame < frames; frame++)
                {
                    if (_activeFrame == null || _activeFrameOffset + channels > _activeFrame.Length)
                    {
                        if (_frames.Count > 0)
                        {
                            _activeFrame = _frames.Dequeue();
                            _activeFrameOffset = 0;
                        }
                        else
                        {
                            _activeFrame = null;
                            _underruns++;
                        }
                    }

                    if (_activeFrame == null)
                    {
                        for (var ch = 0; ch < channels; ch++)
                            buffer[cursor++] = 0f;
                        continue;
                    }

                    for (var ch = 0; ch < channels; ch++)
                        buffer[cursor++] = _activeFrame[_activeFrameOffset++];
                }

                if (cursor < sampleCount)
                {
                        for (var i = cursor; i < sampleCount; i++)
                            buffer[i] = 0f;
                }
            }
            finally
            {
                System.Threading.Monitor.Exit(_lock);
            }
        }
    }
}

