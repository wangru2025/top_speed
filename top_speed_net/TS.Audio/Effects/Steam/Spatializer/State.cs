using System.Numerics;

namespace TS.Audio
{
    internal sealed unsafe partial class SteamAudioSpatialModifier
    {
        public void UpdateSpatialState(Vector3 localPosition, float distanceAttenuation, float airLow, float airMid, float airHigh, float occlusion, float spatialBlend, bool stereoWidening)
        {
            lock (_sync)
            {
                if (_disposed || !_initialized)
                    return;

                _localPosition = localPosition;
                _direction = ToDirection(localPosition);
                _distanceAttenuation = Clamp01(distanceAttenuation);
                _baseAirAbsorption[0] = Clamp01(airLow);
                _baseAirAbsorption[1] = Clamp01(airMid);
                _baseAirAbsorption[2] = Clamp01(airHigh);
                _baseOcclusion = Clamp01(occlusion);
                _spatialBlend = Clamp01(spatialBlend);
                _stereoWidening = stereoWidening;
            }
        }

        public void ApplyDirectSimulation(float occlusion, float airLow, float airMid, float airHigh, float transmissionLow, float transmissionMid, float transmissionHigh)
        {
            lock (_sync)
            {
                if (_disposed || !_initialized)
                    return;

                _hasDirectSimulation = true;
                _simulationOcclusion = Clamp01(occlusion);
                _simulationAirAbsorption[0] = Clamp01(airLow);
                _simulationAirAbsorption[1] = Clamp01(airMid);
                _simulationAirAbsorption[2] = Clamp01(airHigh);
                _simulationTransmission[0] = Clamp01(transmissionLow);
                _simulationTransmission[1] = Clamp01(transmissionMid);
                _simulationTransmission[2] = Clamp01(transmissionHigh);
            }
        }

        public void ClearDirectSimulation()
        {
            lock (_sync)
            {
                if (_disposed || !_initialized)
                    return;

                _hasDirectSimulation = false;
                _simulationOcclusion = 1f;
                _simulationAirAbsorption[0] = 1f;
                _simulationAirAbsorption[1] = 1f;
                _simulationAirAbsorption[2] = 1f;
                _simulationTransmission[0] = 1f;
                _simulationTransmission[1] = 1f;
                _simulationTransmission[2] = 1f;
            }
        }

        public void ApplyReverbSimulation(float timeLow, float timeMid, float timeHigh, float eqLow, float eqMid, float eqHigh, int delay, float wetScale)
        {
            lock (_sync)
            {
                if (_disposed || !_initialized)
                    return;

                _hasReverbSimulation = timeLow > 0f || timeMid > 0f || timeHigh > 0f || wetScale > 0f;
                _reverbTimes[0] = System.Math.Max(0f, timeLow);
                _reverbTimes[1] = System.Math.Max(0f, timeMid);
                _reverbTimes[2] = System.Math.Max(0f, timeHigh);
                _reverbEq[0] = Clamp01(eqLow);
                _reverbEq[1] = Clamp01(eqMid);
                _reverbEq[2] = Clamp01(eqHigh);
                _reverbDelay = System.Math.Max(0, delay);
                _reverbWetTarget = Clamp01(wetScale);
            }
        }

        public void ClearReverbSimulation()
        {
            lock (_sync)
            {
                if (_disposed || !_initialized)
                    return;

                _hasReverbSimulation = false;
                _reverbTimes[0] = 0f;
                _reverbTimes[1] = 0f;
                _reverbTimes[2] = 0f;
                _reverbEq[0] = 1f;
                _reverbEq[1] = 1f;
                _reverbEq[2] = 1f;
                _reverbDelay = 0;
                _reverbWetTarget = 0f;
            }
        }

        public void ClearSimulationState()
        {
            ClearDirectSimulation();
            ClearReverbSimulation();
        }
    }
}
