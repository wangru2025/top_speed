using System;
using System.Linq;
using LiteNetLib;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;
using TopSpeed.Server.Moderation;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void SendProtocolMessage(PlayerConnection player, ProtocolMessageCode code, string text)
        {
            SendStream(player, PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
            {
                Code = code,
                Message = text ?? string.Empty
            }), PacketStream.Direct);
        }

        private void BroadcastGlobalChat(PlayerConnection sender, string message)
        {
            if (!TextChatModeration.TryAllowTextChat(_config.Features, out var moderationMessage))
            {
                SendProtocolMessage(sender, ProtocolMessageCode.Failed, moderationMessage);
                return;
            }

            var trimmed = (message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            var senderName = string.IsNullOrWhiteSpace(sender.Name)
                ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), sender.PlayerNumber + 1)
                : sender.Name.Trim();
            var formatted = LocalizationService.Format(
                LocalizationService.Mark("{0} says: {1}"),
                senderName,
                trimmed);

            var payload = PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
            {
                Code = ProtocolMessageCode.Chat,
                Message = formatted
            });

            foreach (var player in _players.Values)
            {
                if (!player.Connected || player.Handshake != HandshakeState.Complete)
                    continue;
                SendStream(player, payload, PacketStream.Chat);
            }
        }

        private void BroadcastRoomChat(PlayerConnection sender, string message)
        {
            if (!TextChatModeration.TryAllowTextChat(_config.Features, out var moderationMessage))
            {
                SendProtocolMessage(sender, ProtocolMessageCode.Failed, moderationMessage);
                return;
            }

            var trimmed = (message ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            if (!sender.RoomId.HasValue || !_rooms.TryGetValue(sender.RoomId.Value, out var room))
            {
                SendProtocolMessage(sender, ProtocolMessageCode.NotInRoom, LocalizationService.Translate(LocalizationService.Mark("You are not in a game room.")));
                return;
            }

            var senderName = string.IsNullOrWhiteSpace(sender.Name)
                ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), sender.PlayerNumber + 1)
                : sender.Name.Trim();
            var formatted = LocalizationService.Format(
                LocalizationService.Mark("[room]: {0} says: {1}"),
                senderName,
                trimmed);

            var payload = PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
            {
                Code = ProtocolMessageCode.RoomChat,
                Message = formatted
            });

            foreach (var playerId in room.PlayerIds)
            {
                if (!_players.TryGetValue(playerId, out var player))
                    continue;
                if (!player.Connected || player.Handshake != HandshakeState.Complete)
                    continue;
                SendStream(player, payload, PacketStream.Chat);
            }
        }

        private static string DescribePlayer(PlayerConnection player)
        {
            if (!string.IsNullOrWhiteSpace(player.Name))
                return player.Name;
            return LocalizationService.Translate(LocalizationService.Mark("A player"));
        }

    }
}
