using System;
using System.Threading;
using SoundFlow.Abstracts;
using SoundFlow.Enums;
using SoundFlow.Interfaces;
using SfAudioEngine = SoundFlow.Abstracts.AudioEngine;
using SfAudioFormat = SoundFlow.Structs.AudioFormat;

namespace TS.Audio
{
    internal sealed class SourcePlayer : SoundComponent
    {
        private readonly ISoundDataProvider _provider;
        private readonly Action<Exception>? _onAudioThreadException;
        private int _pendingPlaybackEnded;
        private bool _ended;

        public SourcePlayer(SfAudioEngine engine, SfAudioFormat format, ISoundDataProvider provider, Action<Exception>? onAudioThreadException = null)
            : base(engine, format)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _onAudioThreadException = onAudioThreadException;
            Enabled = false;
        }

        public override string Name { get; set; } = "Source";
        public PlaybackState State { get; private set; }
        public bool IsLooping { get; set; }
        public float PlaybackSpeed { get; set; } = 1f;

        public float Time =>
            Format.Channels == 0 || Format.SampleRate == 0
                ? 0f
                : (float)_provider.Position / Format.Channels / Format.SampleRate;

        public float Duration =>
            _provider.Length <= 0 || Format.Channels == 0 || Format.SampleRate == 0
                ? 0f
                : (float)_provider.Length / Format.Channels / Format.SampleRate;

        public void Play()
        {
            _ended = false;
            Interlocked.Exchange(ref _pendingPlaybackEnded, 0);
            Enabled = true;
            State = PlaybackState.Playing;
        }

        public void Pause()
        {
            Enabled = false;
            State = PlaybackState.Paused;
        }

        public void Stop()
        {
            Enabled = false;
            State = PlaybackState.Stopped;
            Seek(0);
            _ended = false;
            Interlocked.Exchange(ref _pendingPlaybackEnded, 0);
        }

        public void Silence()
        {
            Enabled = false;
            State = PlaybackState.Stopped;
            _ended = false;
            Interlocked.Exchange(ref _pendingPlaybackEnded, 0);
        }

        public bool Seek(int sampleOffset)
        {
            if (!_provider.CanSeek)
                return false;

            _provider.Seek(sampleOffset);
            _ended = false;
            Interlocked.Exchange(ref _pendingPlaybackEnded, 0);
            return true;
        }

        public bool ConsumePlaybackEnded()
        {
            return Interlocked.Exchange(ref _pendingPlaybackEnded, 0) != 0;
        }

        protected override void GenerateAudio(Span<float> buffer, int channels)
        {
            if (State != PlaybackState.Playing || channels <= 0)
            {
                buffer.Clear();
                return;
            }

            var output = buffer;
            try
            {
                while (!output.IsEmpty)
                {
                    var read = _provider.ReadBytes(output);
                    if (read > 0)
                    {
                        output = output.Slice(read);
                        continue;
                    }

                    if (IsLooping && _provider.CanSeek && _provider.Length > 0)
                    {
                        _provider.Seek(0);
                        continue;
                    }

                    output.Clear();
                    EndPlayback();
                    return;
                }
            }
            catch (Exception ex)
            {
                output.Clear();
                Silence();
                _onAudioThreadException?.Invoke(ex);
                EndPlayback();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        private void EndPlayback()
        {
            if (_ended)
                return;

            _ended = true;
            Enabled = false;
            State = PlaybackState.Stopped;
            Interlocked.Exchange(ref _pendingPlaybackEnded, 1);
        }
    }
}
