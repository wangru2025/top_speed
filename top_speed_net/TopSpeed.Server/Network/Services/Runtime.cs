using System;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed class Runtime
        {
            private readonly RaceServer _owner;

            public Runtime(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void Update(float deltaSeconds)
            {
                _owner._transport.Pump();

                lock (_owner._lock)
                {
                    _owner.EnsureServerLoopThreadUnsafe();
                    _owner.DrainCommandQueueUnsafe();

                    if (deltaSeconds <= 0f)
                        return;

                    _owner._simulationAccumulator += deltaSeconds;
                    while (_owner._simulationAccumulator >= ServerSimulationStepSeconds)
                    {
                        _owner.DrainCommandQueueUnsafe();
                        _owner._simulationAccumulator -= ServerSimulationStepSeconds;
                        _owner._simulationTick++;
                        _owner._cleanupAccumulator += ServerSimulationStepSeconds;
                        _owner._snapshotAccumulator += ServerSimulationStepSeconds;

                        if (_owner._cleanupAccumulator >= CleanupIntervalSeconds)
                        {
                            _owner._cleanupAccumulator -= CleanupIntervalSeconds;
                            _owner._session.CleanupExpiredConnections();
                        }

                        _owner.UpdateBots(ServerSimulationStepSeconds);
                        _owner.CheckForBumps();
                        _owner._race.UpdateCompletions();

                        if (_owner._snapshotAccumulator >= ServerSnapshotIntervalSeconds)
                        {
                            _owner._snapshotAccumulator -= ServerSnapshotIntervalSeconds;
                            _owner.BroadcastPlayerData();
                        }
                    }

                    _owner.DrainCommandQueueUnsafe();
                }
            }
        }
    }
}
