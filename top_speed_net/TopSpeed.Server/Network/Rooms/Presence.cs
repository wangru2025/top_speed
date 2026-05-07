using System.Collections.Generic;
using System.Linq;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void SetRoomMemberPresence(RaceRoom room, uint playerId, RoomMemberPresenceState presence)
        {
            if (room == null || playerId == 0)
                return;

            room.MemberPresence[playerId] = presence;
        }

        private void RemoveRoomMemberPresence(RaceRoom room, uint playerId)
        {
            if (room == null || playerId == 0)
                return;

            room.MemberPresence.Remove(playerId);
        }

        private bool IsRoomMemberActive(RaceRoom room, uint playerId)
        {
            if (room == null || playerId == 0)
                return false;
            if (!room.PlayerIds.Contains(playerId))
                return false;
            if (!room.MemberPresence.TryGetValue(playerId, out var presence))
                return false;
            if (presence != RoomMemberPresenceState.Active)
                return false;

            return _players.TryGetValue(playerId, out var player)
                && player.Connected
                && player.Handshake == HandshakeState.Complete;
        }

        private IEnumerable<uint> EnumerateActiveHumanPlayerIds(RaceRoom room)
        {
            if (room == null)
                return Enumerable.Empty<uint>();

            return room.PlayerIds.Where(playerId => IsRoomMemberActive(room, playerId));
        }

        private int GetActiveHumanParticipantCount(RaceRoom room)
        {
            if (room == null)
                return 0;

            return room.PlayerIds.Count(playerId => IsRoomMemberActive(room, playerId));
        }

        private int GetActiveParticipantCountForStartBarrier(RaceRoom room)
        {
            if (room == null)
                return 0;

            return GetActiveHumanParticipantCount(room) + room.Bots.Count;
        }
    }
}
