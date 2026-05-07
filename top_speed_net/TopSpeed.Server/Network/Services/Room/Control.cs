using TopSpeed.Localization;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void HandleRoomControl(PlayerConnection player, PacketRoomRaceControl packet)
            {
                if (!TryGetHosted(player, out var room))
                    return;

                switch (packet.Action)
                {
                    case RoomRaceControlAction.CancelPrepare:
                        CancelPrepare(player, room);
                        return;

                    case RoomRaceControlAction.Pause:
                        SetPaused(player, room, paused: true);
                        return;

                    case RoomRaceControlAction.Resume:
                        SetPaused(player, room, paused: false);
                        return;

                    case RoomRaceControlAction.Stop:
                        StopCurrentGame(player, room);
                        return;

                    default:
                        _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Unknown race control request."));
                        return;
                }
            }

            private void CancelPrepare(PlayerConnection player, GameRoom room)
            {
                if (!room.PreparingRace)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Race preparation is not currently active."));
                    return;
                }

                _owner._race.CancelPrepare(room, player);
            }

            private void SetPaused(PlayerConnection player, GameRoom room, bool paused)
            {
                if (!room.RaceStarted)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("A multiplayer race is not currently active."));
                    return;
                }

                if (room.RacePaused == paused)
                {
                    _owner.SendProtocolMessage(
                        player,
                        ProtocolMessageCode.Failed,
                        paused
                            ? LocalizationService.Mark("The current game is already paused.")
                            : LocalizationService.Mark("The current game is not paused."));
                    return;
                }

                _owner._race.SetPaused(room, player, paused);
            }

            private void StopCurrentGame(PlayerConnection player, GameRoom room)
            {
                if (!room.RaceStarted)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("A multiplayer race is not currently active."));
                    return;
                }

                _owner._race.StopWithoutResults(room, player);
            }
        }
    }
}

