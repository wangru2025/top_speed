using TopSpeed.Protocol;
using TopSpeed.Localization;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void BroadcastServerConnectAnnouncement(PlayerConnection connected)
        {
            var name = string.IsNullOrWhiteSpace(connected.Name)
                ? LocalizationService.Translate(LocalizationService.Mark("A player"))
                : connected.Name;
            var text = LocalizationService.Format(LocalizationService.Mark("{0} has connected to the server."), name);
            foreach (var player in _players.Values)
            {
                if (player.Id == connected.Id || !player.ServerPresenceAnnounced)
                    continue;

                SendProtocolMessage(player, ProtocolMessageCode.ServerPlayerConnected, text);
            }
        }

        private void BroadcastServerDisconnectAnnouncement(PlayerConnection disconnected, string reason)
        {
            var name = string.IsNullOrWhiteSpace(disconnected.Name)
                ? LocalizationService.Translate(LocalizationService.Mark("A player"))
                : disconnected.Name;
            var normalizedReason = (reason ?? string.Empty).Trim();
            var text = string.Equals(normalizedReason, "heartbeat_missed", System.StringComparison.OrdinalIgnoreCase)
                ? LocalizationService.Format(LocalizationService.Mark("{0} has lost connection to the server."), name)
                : LocalizationService.Format(LocalizationService.Mark("{0} has disconnected from the server."), name);

            foreach (var player in _players.Values)
            {
                if (player.Id == disconnected.Id || !player.ServerPresenceAnnounced)
                    continue;

                SendProtocolMessage(player, ProtocolMessageCode.ServerPlayerDisconnected, text);
            }
        }

    }
}
