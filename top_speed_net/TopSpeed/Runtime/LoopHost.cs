using System;
using System.Diagnostics;
using System.Threading;

namespace TopSpeed.Runtime
{
    internal sealed class LoopHost : ILoopHost
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private Thread? _thread;
        private volatile bool _running;
        private long _lastTicks;
        private Action<float>? _onTick;
        private Func<int>? _resolveIntervalMs;

        public bool IsRunning => _running;

        public void Start(Action<float> onTick, Func<int> resolveIntervalMs)
        {
            if (onTick == null)
                throw new ArgumentNullException(nameof(onTick));
            if (resolveIntervalMs == null)
                throw new ArgumentNullException(nameof(resolveIntervalMs));
            if (_thread != null)
                return;

            _onTick = onTick;
            _resolveIntervalMs = resolveIntervalMs;
            _running = true;
            _lastTicks = 0L;
            _stopwatch.Restart();

            _thread = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = "GameLoop"
            };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            if (_thread == null)
                return;
            if (_thread.IsAlive)
                _thread.Join(200);
            _thread = null;
            _stopwatch.Stop();
            _onTick = null;
            _resolveIntervalMs = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private void RunLoop()
        {
            while (_running)
            {
                var onTick = _onTick;
                var resolveIntervalMs = _resolveIntervalMs;
                if (onTick == null || resolveIntervalMs == null)
                    break;

                var now = _stopwatch.ElapsedTicks;
                if (_lastTicks == 0L)
                    _lastTicks = now;
                var deltaSeconds = (float)(now - _lastTicks) / Stopwatch.Frequency;
                _lastTicks = now;
                onTick(deltaSeconds);

                var intervalMs = resolveIntervalMs();
                if (intervalMs <= 0)
                    intervalMs = 8;
                Thread.Sleep(intervalMs);
            }
        }
    }
}
