using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed class Chat : IChatService
        {
            private readonly RaceServer _owner;

            public Chat(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void RegisterPackets(ServerPktReg registry)
            {
                registry.Add("chat", Command.ProtocolMessage, (player, payload, endPoint) =>
                {
                    if (!PacketSerializer.TryReadProtocolMessage(payload, out var message))
                    {
                        _owner.PacketFail(endPoint, Command.ProtocolMessage);
                        return;
                    }

                    _owner.HandleGlobalChat(player, message);
                });
            }
        }
    }
}
