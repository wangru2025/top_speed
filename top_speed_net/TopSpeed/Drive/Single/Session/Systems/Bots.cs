using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Single.Session.Systems
{
    internal sealed class Bots : TopSpeed.Drive.Session.Subsystem
    {
        private readonly ComputerPlayer?[] _players;
        private readonly int _playerCount;
        private readonly Vehicles.ICar _car;
        private readonly Tracks.Track _track;
        private readonly int _lapLimit;
        private readonly Func<int> _readRaceTimeMs;
        private readonly Action _updatePositions;
        private readonly Action<int, int> _recordFinish;
        private readonly Action<int> _announceFinishOrder;
        private readonly Func<bool> _checkFinish;
        private readonly Action<float> _queueFinish;

        public Bots(
            string name,
            int order,
            ComputerPlayer?[] players,
            int playerCount,
            Vehicles.ICar car,
            Tracks.Track track,
            int lapLimit,
            Func<int> readRaceTimeMs,
            Action updatePositions,
            Action<int, int> recordFinish,
            Action<int> announceFinishOrder,
            Func<bool> checkFinish,
            Action<float> queueFinish)
            : base(name, order)
        {
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _playerCount = playerCount;
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _lapLimit = lapLimit;
            _readRaceTimeMs = readRaceTimeMs ?? throw new ArgumentNullException(nameof(readRaceTimeMs));
            _updatePositions = updatePositions ?? throw new ArgumentNullException(nameof(updatePositions));
            _recordFinish = recordFinish ?? throw new ArgumentNullException(nameof(recordFinish));
            _announceFinishOrder = announceFinishOrder ?? throw new ArgumentNullException(nameof(announceFinishOrder));
            _checkFinish = checkFinish ?? throw new ArgumentNullException(nameof(checkFinish));
            _queueFinish = queueFinish ?? throw new ArgumentNullException(nameof(queueFinish));
        }

        public override void Update(TopSpeed.Drive.Session.SessionContext context, float elapsed)
        {
            _updatePositions();

            for (var botIndex = 0; botIndex < _playerCount; botIndex++)
            {
                var bot = _players[botIndex];
                if (bot == null)
                    continue;

                bot.Run(elapsed, _car.PositionX, _car.PositionY);
                if (_track.Lap(bot.PositionY) <= _lapLimit || bot.Finished)
                    continue;

                bot.StopAtFinish();
                _recordFinish(bot.PlayerNumber, _readRaceTimeMs());
                _announceFinishOrder(bot.PlayerNumber);
                if (_checkFinish())
                    _queueFinish(context.ProgressSeconds);
            }
        }
    }
}
