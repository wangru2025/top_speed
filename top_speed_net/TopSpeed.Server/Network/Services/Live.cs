using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed class Live : ILiveService
        {
            private readonly RaceServer _owner;

            public Live(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void RegisterPackets(ServerPktReg registry)
            {
                registry.Add("live", Command.PlayerLiveStart, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerLiveStart(payload, out var start))
                        _owner.OnLiveStart(player, start);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerLiveStart);
                });
                registry.Add("live", Command.PlayerLiveFrame, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerLiveFrame(payload, out var frame))
                        _owner.OnLiveFrame(player, frame);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerLiveFrame);
                });
                registry.Add("live", Command.PlayerLiveStop, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerLiveStop(payload, out var stop))
                        _owner.OnLiveStop(player, stop);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerLiveStop);
                });
            }
        }
    }
}
