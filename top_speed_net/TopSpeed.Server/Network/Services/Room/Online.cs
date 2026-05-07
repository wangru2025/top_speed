using System;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static readonly string MainRoomName = LocalizationService.Mark("main room");

        private void HandleOnlinePlayersRequest(PlayerConnection requester)
        {
            var players = _players.Values
                .Where(IsPlayerOnlineVisible)
                .OrderBy(GetOnlineDisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.PlayerNumber)
                .Take(ProtocolConstants.MaxRoomListEntries)
                .Select(BuildOnlinePlayerPacket)
                .ToArray();

            var payload = new PacketOnlinePlayers
            {
                Players = players
            };
            SendStream(requester, PacketSerializer.WriteOnlinePlayers(payload), PacketStream.Query);
        }

        private bool IsPlayerOnlineVisible(PlayerConnection player)
        {
            return player != null && player.Connected && player.Handshake == HandshakeState.Complete;
        }

        private PacketOnlinePlayer BuildOnlinePlayerPacket(PlayerConnection player)
        {
            var room = ResolvePlayerRoom(player);
            return new PacketOnlinePlayer
            {
                PlayerId = player.Id,
                PlayerNumber = player.PlayerNumber,
                Name = GetOnlineDisplayName(player),
                PresenceState = ResolveOnlinePresenceState(player, room),
                RoomName = ResolveOnlineRoomName(room)
            };
        }

        private GameRoom? ResolvePlayerRoom(PlayerConnection player)
        {
            if (!player.RoomId.HasValue)
                return null;
            return _rooms.TryGetValue(player.RoomId.Value, out var room) ? room : null;
        }

        private OnlinePresenceState ResolveOnlinePresenceState(PlayerConnection player, GameRoom? room)
        {
            if (room != null
                && room.RaceStarted
                && (player.State == PlayerState.AwaitingStart || player.State == PlayerState.Racing || player.State == PlayerState.Finished))
            {
                return OnlinePresenceState.Racing;
            }

            if (room != null
                && room.PreparingRace
                && room.PlayerIds.Contains(player.Id)
                && !room.PendingLoadouts.ContainsKey(player.Id)
                && !room.PrepareSkips.Contains(player.Id))
            {
                return OnlinePresenceState.PreparingToRace;
            }

            return OnlinePresenceState.Available;
        }

        private static string ResolveOnlineRoomName(GameRoom? room)
        {
            if (room == null)
                return MainRoomName;
            if (!string.IsNullOrWhiteSpace(room.Name))
                return room.Name;
            return LocalizationService.Format(LocalizationService.Mark("room {0}"), room.Id);
        }

        private static string GetOnlineDisplayName(PlayerConnection player)
        {
            if (!string.IsNullOrWhiteSpace(player.Name))
                return player.Name;
            return LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1);
        }
    }
}

