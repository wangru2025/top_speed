using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed class Media : IMediaService
        {
            private readonly RaceServer _owner;

            public Media(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void RegisterPackets(ServerPktReg registry)
            {
                registry.Add("media", Command.PlayerMediaBegin, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerMediaBegin(payload, out var begin))
                        _owner.OnMediaBegin(player, begin);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerMediaBegin);
                });
                registry.Add("media", Command.PlayerMediaChunk, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerMediaChunk(payload, out var chunk))
                        _owner.OnMediaChunk(player, chunk);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerMediaChunk);
                });
                registry.Add("media", Command.PlayerMediaEnd, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerMediaEnd(payload, out var end))
                        _owner.OnMediaEnd(player, end);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerMediaEnd);
                });
            }
        }
    }
}
