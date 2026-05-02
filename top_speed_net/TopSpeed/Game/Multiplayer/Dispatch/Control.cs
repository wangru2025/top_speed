using System;
using TopSpeed.Localization;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private void RegisterControl()
            {
                _reg.Add("control", Command.Disconnect, HandleDisconnect);
                _reg.Add("control", Command.PlayerNumber, HandlePlayerNumber);
                _reg.Add("control", Command.Pong, HandlePong);
            }

            private bool HandleDisconnect(IncomingPacket packet)
            {
                var message = LocalizationService.Mark("Disconnected from server.");
                var explicitDisconnect = ClientPacketSerializer.TryReadDisconnect(packet.Payload, out var disconnectMessage);
                if (explicitDisconnect && !string.IsNullOrWhiteSpace(disconnectMessage))
                {
                    message = disconnectMessage;
                }

                _owner.HandleMultiplayerDisconnect(message, explicitDisconnect);
                return true;
            }

            private bool HandlePlayerNumber(IncomingPacket packet)
            {
                var session = _owner._session;
                if (session == null)
                    return false;

                if (ClientPacketSerializer.TryReadPlayer(packet.Payload, out var assigned) && assigned.PlayerId == session.PlayerId)
                    session.UpdatePlayerNumber(assigned.PlayerNumber);
                return true;
            }

            private bool HandlePong(IncomingPacket packet)
            {
                _owner._multiplayerCoordinator.HandlePingReply(packet.ReceivedUtcTicks);
                return true;
            }
        }
    }
}
