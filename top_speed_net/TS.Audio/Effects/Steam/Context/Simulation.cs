using System;
using System.Collections.Generic;
using System.Numerics;
using SteamAudio;

namespace TS.Audio
{
    internal sealed unsafe partial class SteamAudioRuntime
    {
        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up)
        {
            if (_context.Handle == IntPtr.Zero)
                return;

            var normForward = NormalizeOrFallback(forward, new Vector3(0f, 0f, 1f));
            var normUp = NormalizeOrFallback(up, new Vector3(0f, 1f, 0f));
            var right = NormalizeOrFallback(Vector3.Cross(normUp, normForward), new Vector3(1f, 0f, 0f));

            _listenerState = new ListenerState(
                ToIpl(right),
                ToIpl(normUp),
                new IPL.Vector3(-normForward.X, -normForward.Y, -normForward.Z),
                ToIpl(position));
        }

        public void SetScene(IPL.Scene scene)
        {
            if (_context.Handle == IntPtr.Zero || scene.Handle == IntPtr.Zero)
                return;

            lock (_simulationLock)
            {
                EnsureSimulator();
                _scene = scene;
                if (_simulator.Handle == IntPtr.Zero)
                    return;

                IPL.SimulatorSetScene(_simulator, _scene);
                IPL.SimulatorCommit(_simulator);
            }
        }

        public void ClearScene()
        {
            lock (_simulationLock)
            {
                if (_simulator.Handle != IntPtr.Zero)
                {
                    IPL.SimulatorSetScene(_simulator, default);
                    IPL.SimulatorCommit(_simulator);
                }

                _scene = default;
            }
        }

        public void UpdateSimulation(IReadOnlyList<AudioSourceHandle> sources)
        {
            if (_context.Handle == IntPtr.Zero || sources == null || sources.Count == 0)
                return;

            lock (_simulationLock)
            {
                _activeSources.Clear();
                for (var i = 0; i < sources.Count; i++)
                {
                    var source = sources[i];
                    if (source == null || !source.IsActive || !source.IsSpatialized || !source.UsesSteamAudio || !source.IsPlaying)
                        continue;

                    _activeSources.Add(source);
                }

                if (_activeSources.Count == 0)
                {
                    if (_sources.Count > 0)
                        RemoveInactiveSources(_activeSources);
                    return;
                }

                if (_simulator.Handle == IntPtr.Zero || _scene.Handle == IntPtr.Zero)
                {
                    if (_sources.Count > 0)
                    {
                        _activeSources.Clear();
                        RemoveInactiveSources(_activeSources);
                    }

                    for (var i = 0; i < sources.Count; i++)
                    {
                        var source = sources[i];
                        if (source != null && source.IsActive && source.IsSpatialized && source.UsesSteamAudio && source.IsPlaying)
                            ApplyRoomOnlyOutputs(source);
                    }
                    return;
                }

                foreach (var source in _activeSources)
                {
                    EnsureSource(source);
                    SetSourceInputs(source);
                }

                RemoveInactiveSources(_activeSources);

                var shared = BuildSharedInputs();
                var flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections;
                IPL.SimulatorSetSharedInputs(_simulator, flags, in shared);
                IPL.SimulatorCommit(_simulator);
                IPL.SimulatorRunDirect(_simulator);
                IPL.SimulatorRunReflections(_simulator);

                foreach (var source in _activeSources)
                {
                    if (!_sources.TryGetValue(source, out var simulationSource) || simulationSource.Handle == IntPtr.Zero)
                        continue;

                    IPL.SourceGetOutputs(simulationSource, flags, out var outputs);
                    ApplyDirectOutputs(source, in outputs.Direct);
                    ApplyReverbOutputs(source, in outputs.Reflections);
                }
            }
        }

        private void EnsureSimulator()
        {
            if (_simulator.Handle != IntPtr.Zero || _context.Handle == IntPtr.Zero)
                return;

            var settings = new IPL.SimulationSettings
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections,
                SceneType = IPL.SceneType.Default,
                ReflectionType = ReflectionType,
                MaxNumOcclusionSamples = 32,
                MaxNumRays = 2048,
                NumDiffuseSamples = 64,
                MaxDuration = ReflectionDurationSeconds,
                MaxOrder = ReflectionOrder,
                MaxNumSources = 128,
                NumThreads = Math.Max(1, Environment.ProcessorCount - 1),
                RayBatchSize = 64,
                NumVisSamples = 8,
                SamplingRate = AudioSettings.SamplingRate,
                FrameSize = AudioSettings.FrameSize
            };

            if (IPL.SimulatorCreate(_context, in settings, out _simulator) != IPL.Error.Success)
                _simulator = default;
        }

        private void EnsureSource(AudioSourceHandle source)
        {
            if (_sources.TryGetValue(source, out var existing) && existing.Handle != IntPtr.Zero)
                return;

            var settings = new IPL.SourceSettings
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections
            };

            if (IPL.SourceCreate(_simulator, in settings, out var simulationSource) != IPL.Error.Success || simulationSource.Handle == IntPtr.Zero)
                return;

            IPL.SourceAdd(simulationSource, _simulator);
            _sources[source] = simulationSource;
        }

        private void RemoveInactiveSources(HashSet<AudioSourceHandle> active)
        {
            if (_sources.Count == 0)
                return;

            _sourcesToRemove.Clear();
            foreach (var entry in _sources)
            {
                if (!active.Contains(entry.Key))
                    _sourcesToRemove.Add(entry.Key);
            }

            for (var i = 0; i < _sourcesToRemove.Count; i++)
            {
                var handle = _sourcesToRemove[i];
                if (_sources.TryGetValue(handle, out var simulationSource) && simulationSource.Handle != IntPtr.Zero)
                {
                    if (_simulator.Handle != IntPtr.Zero)
                        IPL.SourceRemove(simulationSource, _simulator);
                    IPL.SourceRelease(ref simulationSource);
                }

                handle.ClearDirectSimulation();
                handle.ClearReverbSimulation();
                _sources.Remove(handle);
            }

            _sourcesToRemove.Clear();
        }

        private void SetSourceInputs(AudioSourceHandle handle)
        {
            if (!_sources.TryGetValue(handle, out var source) || source.Handle == IntPtr.Zero)
                return;

            var position = ToIpl(handle.WorldPosition);
            var distanceModel = new IPL.DistanceAttenuationModel
            {
                Type = handle.DistanceModel == DistanceModel.Inverse
                    ? IPL.DistanceAttenuationModelType.InverseDistance
                    : IPL.DistanceAttenuationModelType.Callback,
                MinDistance = handle.ReferenceDistance,
                Callback = null,
                UserData = IntPtr.Zero,
                Dirty = 0
            };

            var airModel = new IPL.AirAbsorptionModel
            {
                Type = IPL.AirAbsorptionModelType.Default
            };
            airModel.Coefficients[0] = 1f;
            airModel.Coefficients[1] = 1f;
            airModel.Coefficients[2] = 1f;

            var inputs = new IPL.SimulationInputs
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections,
                DirectFlags = IPL.DirectSimulationFlags.Occlusion | IPL.DirectSimulationFlags.Transmission | IPL.DirectSimulationFlags.AirAbsorption,
                Source = new IPL.CoordinateSpace3
                {
                    Origin = position,
                    Right = new IPL.Vector3(1f, 0f, 0f),
                    Up = new IPL.Vector3(0f, 1f, 0f),
                    Ahead = new IPL.Vector3(0f, 0f, 1f)
                },
                DistanceAttenuationModel = distanceModel,
                AirAbsorptionModel = airModel,
                Directivity = new IPL.Directivity { DipoleWeight = 0f, DipolePower = 1f },
                OcclusionType = IPL.OcclusionType.Raycast,
                OcclusionRadius = 0.5f,
                NumOcclusionSamples = 8,
                HybridReverbTransitionTime = 0.25f,
                HybridReverbOverlapPercent = 0.25f,
                NumTransmissionRays = 4
            };

            inputs.ReverbScale[0] = 1f;
            inputs.ReverbScale[1] = 1f;
            inputs.ReverbScale[2] = 1f;

            IPL.SourceSetInputs(source, inputs.Flags, in inputs);
        }

        private IPL.SimulationSharedInputs BuildSharedInputs()
        {
            var listener = _listenerState;
            return new IPL.SimulationSharedInputs
            {
                Listener = new IPL.CoordinateSpace3
                {
                    Origin = listener.Origin,
                    Right = listener.Right,
                    Up = listener.Up,
                    Ahead = listener.Ahead
                },
                NumRays = 2048,
                NumBounces = 2,
                Duration = ReflectionDurationSeconds,
                Order = ReflectionOrder,
                IrradianceMinDistance = 1f
            };
        }
    }
}
