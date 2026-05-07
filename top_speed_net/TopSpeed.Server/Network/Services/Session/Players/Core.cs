using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Moderation;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void HandlePlayerHello(PlayerConnection player, PacketPlayerHello hello)
        {
            var finalizeHandshake = player.Handshake == HandshakeState.AwaitingPlayerHello;
            var knownNames = CollectKnownPlayerNames();
            var validation = NameModeration.Validate(_config.Moderation, hello.Name, player.Id, knownNames);
            if (!validation.Accepted)
            {
                RemoveConnection(
                    player,
                    notifyRoom: false,
                    sendDisconnectPacket: true,
                    reason: validation.RejectReasonCode,
                    disconnectMessage: validation.RejectMessage,
                    announcePresenceDisconnect: false);
                return;
            }

            player.Name = validation.NormalizedName;
            player.MarkPlayerIdentified();
            if (finalizeHandshake)
            {
                player.Handshake = HandshakeState.Complete;
                player.MarkSessionReady();
                _session.SendInitialConnectionState(player);
                player.MarkActive();
            }

            if (!player.ServerPresenceAnnounced)
            {
                player.ServerPresenceAnnounced = true;
                BroadcastServerConnectAnnouncement(player);
            }
            if (player.RoomId.HasValue && _rooms.TryGetValue(player.RoomId.Value, out var room))
            {
                SetRoomMemberPresence(room, player.Id, RoomMemberPresenceState.Active);
                _room.TouchVersion(room);
                _notify.RoomParticipant(
                    room,
                    RoomEventKind.ParticipantStateChanged,
                    player.Id,
                    player.PlayerNumber,
                    player.State,
                    string.IsNullOrWhiteSpace(player.Name)
                        ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1)
                        : player.Name);
                _notify.BroadcastRoomState(room);
            }
        }

        private List<ModerationNameEntry> CollectKnownPlayerNames()
        {
            var result = new List<ModerationNameEntry>(_players.Count);
            foreach (var candidate in _players.Values)
            {
                if (!candidate.ServerPresenceAnnounced)
                    continue;

                result.Add(new ModerationNameEntry(candidate.Id, candidate.Name));
            }

            return result;
        }
    }
}
