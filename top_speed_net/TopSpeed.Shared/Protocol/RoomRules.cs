namespace TopSpeed.Protocol
{
    public static class RoomRules
    {
        public static GameRoomType NormalizeType(GameRoomType roomType)
        {
            return roomType switch
            {
                GameRoomType.OneOnOne => GameRoomType.OneOnOne,
                GameRoomType.PlayersRace => GameRoomType.PlayersRace,
                _ => GameRoomType.BotsRace
            };
        }

        public static byte NormalizePlayersToStart(GameRoomType roomType, byte playersToStart)
        {
            var normalizedType = NormalizeType(roomType);
            if (normalizedType == GameRoomType.OneOnOne)
                return 2;

            return playersToStart >= 2 && playersToStart <= ProtocolConstants.MaxRoomPlayersToStart
                ? playersToStart
                : (byte)2;
        }

        public static RoomRaceState NormalizeRaceState(RoomRaceState state)
        {
            return state switch
            {
                RoomRaceState.Preparing => RoomRaceState.Preparing,
                RoomRaceState.Racing => RoomRaceState.Racing,
                RoomRaceState.Completed => RoomRaceState.Completed,
                RoomRaceState.Aborted => RoomRaceState.Aborted,
                _ => RoomRaceState.Lobby
            };
        }

        public static PlayerState NormalizeParticipantState(PlayerState state)
        {
            return state switch
            {
                PlayerState.NotReady => PlayerState.NotReady,
                PlayerState.AwaitingStart => PlayerState.AwaitingStart,
                PlayerState.Racing => PlayerState.Racing,
                PlayerState.Finished => PlayerState.Finished,
                _ => PlayerState.NotReady
            };
        }
    }
}
