using System;
using TopSpeed.Data;

namespace TopSpeed.Protocol
{
    public static class RaceDistanceRules
    {
        public static float CalculateLapDistance(TrackDefinition[]? definitions)
        {
            if (definitions == null || definitions.Length == 0)
                return 0f;

            var lapDistance = 0f;
            for (var i = 0; i < definitions.Length; i++)
            {
                var length = definitions[i].Length;
                if (float.IsNaN(length) || float.IsInfinity(length))
                    continue;
                lapDistance += Math.Max(1f, length);
            }

            return lapDistance;
        }

        public static float CalculateRaceDistance(TrackDefinition[]? definitions, int roomLaps, int trackLaps)
        {
            var lapDistance = CalculateLapDistance(definitions);
            if (lapDistance <= 0f)
                return 0f;

            var laps = trackLaps > 0 ? trackLaps : roomLaps > 0 ? roomLaps : 1;
            return lapDistance * laps;
        }

        public static bool HasCrossedFinish(float positionY, float raceDistance)
        {
            return !float.IsNaN(positionY)
                && !float.IsInfinity(positionY)
                && !float.IsNaN(raceDistance)
                && !float.IsInfinity(raceDistance)
                && raceDistance > 0f
                && positionY >= raceDistance;
        }
    }
}
