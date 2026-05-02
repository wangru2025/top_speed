using System;
using System.Numerics;

namespace TS.Audio
{
    public sealed class Source : IDisposable
    {
        private readonly AudioSourceHandle _handle;
        private readonly bool _ownsHandle;

        internal Source(AudioSourceHandle handle, bool ownsHandle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _ownsHandle = ownsHandle;
        }

        public bool IsPlaying => _handle.IsPlaying;
        public bool IsPaused => _handle.IsPaused;
        public bool IsDisposed => _handle.IsDisposed;
        public int InputChannels => _handle.InputChannels;
        public int InputSampleRate => _handle.InputSampleRate;
        public float LengthSeconds => _handle.IsDisposed ? 0f : _handle.GetLengthSeconds();
        public AudioSourceSnapshot CaptureSnapshot() => _handle.CaptureSnapshot();

        public void Play(bool loop = false)
        {
            InvokeIfAlive(() => _handle.Play(loop));
        }

        public void Play(bool loop, float fadeInSeconds)
        {
            InvokeIfAlive(() => _handle.Play(loop, fadeInSeconds));
        }

        public void Stop() => _handle.Stop();
        public void Stop(float fadeOutSeconds) => _handle.Stop(fadeOutSeconds);

        public void Pause()
        {
            InvokeIfAlive(() => _handle.Pause());
        }

        public void Resume()
        {
            InvokeIfAlive(() => _handle.Resume());
        }

        public void FadeIn(float seconds)
        {
            InvokeIfAlive(() => _handle.FadeIn(seconds));
        }

        public void FadeOut(float seconds) => _handle.FadeOut(seconds);

        public void SeekToStart()
        {
            InvokeIfAlive(() => _handle.SeekToStart());
        }

        public void SetLooping(bool looping)
        {
            InvokeIfAlive(() => _handle.SetLooping(looping));
        }

        public void SetOnEnd(Action onEnd)
        {
            InvokeIfAlive(() => _handle.SetOnEnd(onEnd));
        }

        public void SetVolume(float value) => InvokeIfAlive(() => _handle.SetVolume(value));
        public float GetVolume() => _handle.GetVolume();
        public void SetPitch(float value) => InvokeIfAlive(() => _handle.SetPitch(value));
        public float GetPitch() => _handle.GetPitch();
        public void SetPan(float value) => InvokeIfAlive(() => _handle.SetPan(value));
        public void SetStereoWidening(bool enabled) => InvokeIfAlive(() => _handle.SetStereoWidening(enabled));

        public void SetPosition(Vector3 position) => InvokeIfAlive(() => _handle.SetPosition(position));
        public void SetVelocity(Vector3 velocity) => InvokeIfAlive(() => _handle.SetVelocity(velocity));
        public void SetTransform(Vector3 position, Vector3 velocity) => InvokeIfAlive(() => _handle.SetTransform(position, velocity));
        public void SetDistanceModel(DistanceModel model, float minDistance, float maxDistance, float rollOff) => InvokeIfAlive(() => _handle.SetDistanceModel(model, minDistance, maxDistance, rollOff));
        public void SetCurveDistanceScaler(float value) => InvokeIfAlive(() => _handle.ApplyCurveDistanceScaler(value));
        public void SetDopplerFactor(float value) => InvokeIfAlive(() => _handle.SetDopplerFactor(value));
        public void SetRoomAcoustics(RoomAcoustics acoustics) => InvokeIfAlive(() => _handle.SetRoomAcoustics(acoustics));

        internal void AddEndObserver(Action onEnd) => InvokeIfAlive(() => _handle.AddEndObserver(onEnd));
        internal void RemoveEndObserver(Action onEnd) => InvokeIfAlive(() => _handle.RemoveEndObserver(onEnd));

        public void Dispose()
        {
            if (_ownsHandle)
                _handle.Dispose();
        }

        private void InvokeIfAlive(Action action)
        {
            if (_handle.IsDisposed)
                return;

            try
            {
                action();
            }
            catch (ObjectDisposedException ex) when (ex.ObjectName == nameof(AudioSourceHandle))
            {
            }
        }
    }
}
