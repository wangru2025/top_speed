using System.Linq;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void HandleStateRequest(PlayerConnection player)
            {
                if (!player.RoomId.HasValue)
                {
                    _owner._notify.SendRoomState(player, null);
                    return;
                }

                if (_owner._rooms.TryGetValue(player.RoomId.Value, out var room))
                {
                    _owner._notify.SendRoomState(player, room);
                    if (room.RaceState == RoomRaceState.Completed)
                        _owner._notify.SendRaceCompletionTo(player, room);
                    else if (room.RaceState == RoomRaceState.Aborted)
                        _owner._notify.ReplayRoomEventsTo(player, room, afterSequence: 0);
                }
                else
                    _owner._notify.SendRoomState(player, null);
            }

            public void HandleGetRequest(PlayerConnection player, PacketRoomGetRequest packet)
            {
                if (!_owner._rooms.TryGetValue(packet.RoomId, out var room))
                {
                    _owner._notify.SendRoomGet(player, null);
                    return;
                }

                _owner._notify.SendRoomGet(player, room);
            }

            public uint TouchVersion(GameRoom room)
            {
                if (room == null)
                    return 0;

                room.Version++;
                if (room.Version == 0)
                    room.Version = 1;
                return room.Version;
            }

            public int FindFreeNumber(GameRoom room)
            {
                for (var i = 0; i < room.PlayersToStart; i++)
                {
                    var usedByPlayer = room.PlayerIds.Any(id => _owner._players.TryGetValue(id, out var player) && player.PlayerNumber == i);
                    var usedByBot = room.Bots.Any(bot => bot.PlayerNumber == i);
                    if (!usedByPlayer && !usedByBot)
                        return i;
                }

                return 0;
            }
        }
    }
}

