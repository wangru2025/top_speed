using System;
using System.Collections.Generic;
using System.Numerics;

namespace TS.Audio
{
    public sealed partial class AudioSourceHandle
    {
        public void SetVolume(float value)
        {
            if (_disposed)
                return;

            lock (_stateSync)
            {
                _userVolume = Clamp01(value);
                if (_fadeRemaining > 0f && !_stopAfterFade)
                {
                    _fadeTargetVolume = _userVolume;
                }
                else
                {
                    _currentVolume = _userVolume;
                }
            }

            QueueApplyControlState();

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourceVolumeChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourceVolumeChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source volume changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["volume"] = _currentVolume,
                        ["volumeDb"] = AudioMath.GainToDecibels(_currentVolume)
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public float GetVolume()
        {
            return _currentVolume;
        }

        public void SetPitch(float value)
        {
            if (_disposed)
                return;

            lock (_stateSync)
            {
                _pitch = value <= 0f ? 0.001f : value;
            }

            QueueApplyControlState();

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourcePitchChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourcePitchChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source pitch changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["pitch"] = _pitch
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public float GetPitch()
        {
            return _pitch;
        }

        public void SetPan(float value)
        {
            if (_disposed)
                return;

            lock (_stateSync)
            {
                _pan = Clamp(value, -1f, 1f);
            }

            QueueApplyControlState();

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourcePanChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourcePanChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source pan changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["pan"] = _pan
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public float GetPan()
        {
            return _pan;
        }

        public void SetStereoWidening(bool enabled)
        {
            if (_disposed)
                return;

            lock (_stateSync)
                _stereoWidening = enabled;

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourceStereoWideningChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourceStereoWideningChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source stereo widening changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["enabled"] = enabled
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (_disposed)
                return;

            lock (_stateSync)
                _position = position;

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourcePositionChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourcePositionChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source position changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["x"] = position.X,
                        ["y"] = position.Y,
                        ["z"] = position.Z
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public void SetVelocity(Vector3 velocity)
        {
            if (_disposed)
                return;

            lock (_stateSync)
                _velocity = velocity;

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourceVelocityChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourceVelocityChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source velocity changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["x"] = velocity.X,
                        ["y"] = velocity.Y,
                        ["z"] = velocity.Z
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public void SetTransform(Vector3 position, Vector3 velocity)
        {
            if (_disposed)
                return;

            lock (_stateSync)
            {
                if (_position == position && _velocity == velocity)
                    return;

                _position = position;
                _velocity = velocity;
            }

            var emitPosition = ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourcePositionChanged);
            var emitVelocity = ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourceVelocityChanged);
            if (!emitPosition && !emitVelocity)
                return;

            if (emitPosition)
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourcePositionChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source position changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["x"] = position.X,
                        ["y"] = position.Y,
                        ["z"] = position.Z
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }

            if (emitVelocity)
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourceVelocityChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source velocity changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["x"] = velocity.X,
                        ["y"] = velocity.Y,
                        ["z"] = velocity.Z
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public void SetDistanceModel(DistanceModel model, float minDistance, float maxDistance, float rollOff)
        {
            if (_disposed)
                return;

            lock (_stateSync)
            {
                _distanceModel = model;
                _minDistance = minDistance;
                _maxDistance = maxDistance;
                _rollOff = rollOff;
            }

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourceDistanceModelChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourceDistanceModelChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source distance model changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["distanceModel"] = model.ToString(),
                        ["minDistance"] = minDistance,
                        ["maxDistance"] = maxDistance,
                        ["rollOff"] = rollOff
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public void ApplyCurveDistanceScaler(float value)
        {
            if (_disposed)
                return;

            lock (_stateSync)
                _curveDistanceScaler = value;
        }

        public void SetDopplerFactor(float value)
        {
            if (_disposed)
                return;

            lock (_stateSync)
                _dopplerFactor = value;

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourceDopplerChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourceDopplerChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source doppler factor changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["dopplerFactor"] = value
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public void SetRoomAcoustics(RoomAcoustics acoustics)
        {
            if (_disposed)
                return;

            lock (_stateSync)
                _roomAcoustics = acoustics;

            if (ShouldEmitSourceDiagnostic(AudioDiagnosticKind.SourceRoomAcousticsChanged))
            {
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.SourceRoomAcousticsChanged,
                    AudioDiagnosticEntityType.Source,
                    _output.Name,
                    _bus.Name,
                    SourceId,
                    "Audio source room acoustics changed.",
                    new Dictionary<string, object?>
                    {
                        ["sourceId"] = SourceId,
                        ["hasRoom"] = acoustics.HasRoom,
                        ["reverbTimeSeconds"] = acoustics.ReverbTimeSeconds
                    },
                    () => new AudioDiagnosticSnapshot(source: CaptureSnapshot()));
            }
        }

        public void Update(double deltaTime)
        {
            if (_disposed)
                return;

            DispatchPlaybackEndedIfNeeded();
            EmitStartedDiagnosticIfNeeded();
            UpdateFade(deltaTime);

            if (!_spatialize)
                return;

            Vector3 position;
            Vector3 velocity;
            DistanceModel distanceModel;
            float minDistance;
            float maxDistance;
            float rollOff;
            float? curveDistanceScaler;
            float dopplerFactor;
            RoomAcoustics roomAcoustics;
            bool stereoWidening;

            lock (_stateSync)
            {
                position = _position;
                velocity = _velocity;
                distanceModel = _distanceModel;
                minDistance = _minDistance;
                maxDistance = _maxDistance;
                rollOff = _rollOff;
                curveDistanceScaler = _curveDistanceScaler;
                dopplerFactor = _dopplerFactor;
                roomAcoustics = _roomAcoustics;
                stereoWidening = _stereoWidening;
            }

            var listener = _output.CaptureListenerState();
            var localPosition = GetLocalPosition(position, listener);
            var distance = localPosition.Length();
            var attenuation = CalculateDistanceAttenuation(distance, distanceModel, minDistance, maxDistance, rollOff, curveDistanceScaler);
            var spatialBlend = _allowHrtf && _output.IsHrtfActive ? 1f : 0f;
            var air = CalculateAirAbsorption(distance, roomAcoustics);
            var occlusion = roomAcoustics.OcclusionOverride ?? 1f;
            var spatialPan = _steamAudioSpatial?.IsBinauralActive == true
                ? 0f
                : Clamp(localPosition.X / Math.Max(1f, distance), -1f, 1f);
            var spatialPitch = CalculateDoppler(position, velocity, listener, localPosition, dopplerFactor);
            var spatialGain = _steamAudioSpatial?.UsesSteamAudio == true ? 1f : attenuation;

            lock (_stateSync)
            {
                _spatialPan = spatialPan;
                _spatialPitch = spatialPitch;
                _spatialGain = spatialGain;
                _distanceAttenuation = attenuation;
                ApplyPan();
                ApplyPlaybackSpeed();
            }

            _steamAudioSpatial?.UpdateSpatialState(localPosition, attenuation, air.Low, air.Mid, air.High, occlusion, spatialBlend, stereoWidening);
        }

        public float GetLengthSeconds()
        {
            if (_disposed)
                return _lastSnapshot?.LengthSeconds ?? _asset.LengthSeconds;

            return _asset.LengthSeconds > 0f ? _asset.LengthSeconds : _player.Duration;
        }

        private static Vector3 GetLocalPosition(Vector3 position, ListenerStateSnapshot listener)
        {
            var delta = position - listener.Position;
            var forward = NormalizeOrFallback(listener.Forward, new Vector3(0f, 0f, 1f));
            var up = NormalizeOrFallback(listener.Up, new Vector3(0f, 1f, 0f));
            var right = Vector3.Cross(up, forward);
            right = NormalizeOrFallback(right, new Vector3(1f, 0f, 0f));
            up = NormalizeOrFallback(Vector3.Cross(forward, right), up);
            return new Vector3(
                Vector3.Dot(delta, right),
                Vector3.Dot(delta, up),
                -Vector3.Dot(delta, forward));
        }

        private static float CalculateDistanceAttenuation(float distance, DistanceModel distanceModel, float minDistance, float maxDistance, float rollOff, float? curveDistanceScaler)
        {
            minDistance = GetEffectiveMinDistance(minDistance, curveDistanceScaler);
            maxDistance = GetEffectiveMaxDistance(minDistance, maxDistance, curveDistanceScaler);

            if (distance <= minDistance)
                return 1f;

            return distanceModel switch
            {
                DistanceModel.Inverse => Clamp01(minDistance / (minDistance + (rollOff * (distance - minDistance)))),
                DistanceModel.Exponential => Clamp01(MathF.Pow(distance / minDistance, -Math.Max(0.001f, rollOff))),
                _ => distance >= maxDistance
                    ? 0f
                    : Clamp01(1f - ((distance - minDistance) / Math.Max(0.001f, maxDistance - minDistance)) * rollOff)
            };
        }

        private float CalculateDoppler(Vector3 position, Vector3 velocity, ListenerStateSnapshot listener, Vector3 localPosition, float dopplerFactor)
        {
            if (dopplerFactor <= 0f)
                return 1f;

            var lengthSquared = localPosition.LengthSquared();
            if (lengthSquared <= 1e-6f)
                return 1f;

            var direction = Vector3.Normalize(position - listener.Position);
            var listenerProjection = Vector3.Dot(listener.Velocity, direction);
            var sourceProjection = Vector3.Dot(velocity, direction);
            var speedOfSound = Math.Max(1f, _output.SystemConfig.SpeedOfSound);
            var factor = Math.Max(0f, dopplerFactor * _output.SystemConfig.DopplerFactor);
            var numerator = speedOfSound - (factor * listenerProjection);
            var denominator = speedOfSound - (factor * sourceProjection);
            if (Math.Abs(denominator) < 0.001f)
                return 1f;

            return Clamp(numerator / denominator, 0.5f, 2f);
        }

        private static (float Low, float Mid, float High) CalculateAirAbsorption(float distance, RoomAcoustics roomAcoustics)
        {
            if (roomAcoustics.AirAbsorptionOverrideLow.HasValue
                || roomAcoustics.AirAbsorptionOverrideMid.HasValue
                || roomAcoustics.AirAbsorptionOverrideHigh.HasValue)
            {
                return (
                    Clamp01(roomAcoustics.AirAbsorptionOverrideLow ?? 1f),
                    Clamp01(roomAcoustics.AirAbsorptionOverrideMid ?? 1f),
                    Clamp01(roomAcoustics.AirAbsorptionOverrideHigh ?? 1f));
            }

            var scale = roomAcoustics.HasRoom
                ? Math.Max(0f, roomAcoustics.AirAbsorptionScale)
                : 0f;
            if (scale <= 0f)
                return (1f, 1f, 1f);

            return (
                Clamp01(MathF.Exp(-distance * 0.0004f * scale)),
                Clamp01(MathF.Exp(-distance * 0.0010f * scale)),
                Clamp01(MathF.Exp(-distance * 0.0025f * scale)));
        }

        private static float GetEffectiveMinDistance(float minDistance, float? curveDistanceScaler)
        {
            minDistance = Math.Max(0.001f, minDistance);
            if (curveDistanceScaler.HasValue && curveDistanceScaler.Value > 0f)
                minDistance *= curveDistanceScaler.Value;
            return minDistance;
        }

        private static float GetEffectiveMaxDistance(float minDistance, float maxDistance, float? curveDistanceScaler)
        {
            maxDistance = Math.Max(minDistance, maxDistance);
            if (curveDistanceScaler.HasValue && curveDistanceScaler.Value > 0f)
                maxDistance *= curveDistanceScaler.Value;
            if (maxDistance < minDistance)
                maxDistance = minDistance;
            return maxDistance;
        }
    }
}
