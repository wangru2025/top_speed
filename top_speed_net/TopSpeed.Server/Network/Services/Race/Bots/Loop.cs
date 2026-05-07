using System;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void UpdateBots(float deltaSeconds)
        {
            foreach (var room in _rooms.Values)
            {
                if (!room.RaceStarted)
                    continue;
                if (room.RacePaused)
                    continue;
                if (room.TrackData == null)
                    continue;

                var definitions = room.TrackData.Definitions;
                if (definitions == null || definitions.Length == 0)
                    continue;

                var laneHalfWidth = GetLaneHalfWidth(room);
                var roadModel = new RoadModel(definitions, laneHalfWidth);
                var raceDistance = GetRaceDistance(room);
                if (roadModel.LapDistance <= 0f || raceDistance <= 0f)
                    continue;

                foreach (var bot in room.Bots)
                {
                    UpdateBotSignals(bot, deltaSeconds);

                    if (bot.State == PlayerState.Finished || bot.State == PlayerState.NotReady)
                        continue;

                    if (TryAdvanceAwaitingStart(room, bot, deltaSeconds))
                        continue;

                    if (bot.State != PlayerState.Racing)
                        continue;

                    if (TryAdvanceCrashPhase(room, bot, deltaSeconds))
                        continue;
                    if (TryAdvanceRestartPhase(room, bot, deltaSeconds))
                        continue;

                    SimulateBotRaceStep(room, bot, roadModel, raceDistance, deltaSeconds);
                }
            }
        }

    }
}
