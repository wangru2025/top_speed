using System;
using System.Collections.Generic;
using TS.Audio;

namespace TopSpeed.Drive.Session.Audio
{
    internal sealed class Queue
    {
        private readonly Queue<Source> _items = new Queue<Source>();
        private readonly object _lock = new object();
        private Source? _current;
        private bool _paused;

        public bool IsIdle
        {
            get
            {
                lock (_lock)
                    return _current == null && _items.Count == 0;
            }
        }

        public void Enqueue(Source sound)
        {
            lock (_lock)
            {
                _items.Enqueue(sound);
                if (_current == null && !_paused)
                    PlayNextLocked();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
                _current?.Stop();
                _current = null;
                _paused = false;
            }
        }

        public void Pause()
        {
            lock (_lock)
            {
                _paused = true;
                _current?.Pause();
            }
        }

        public void Resume()
        {
            lock (_lock)
            {
                if (!_paused)
                    return;

                _paused = false;
                if (_current != null)
                    _current.Resume();
                else
                    PlayNextLocked();
            }
        }

        private void PlayNextLocked()
        {
            if (_paused)
                return;

            if (_items.Count == 0)
            {
                _current = null;
                return;
            }

            var next = _items.Dequeue();
            _current = next;
            next.Stop();
            next.SeekToStart();
            next.SetOnEnd(() => OnEnd(next));
            next.Play(loop: false);
        }

        private void OnEnd(Source finished)
        {
            lock (_lock)
            {
                if (!ReferenceEquals(_current, finished))
                    return;

                _current = null;
                PlayNextLocked();
            }
        }
    }
}
