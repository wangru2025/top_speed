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
                registry.Add("media", Command.PlayerCommunicatorMediaBegin, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerCommunicatorMediaBegin(payload, out var begin))
                        _owner.OnCommunicatorMediaBegin(player, begin);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerCommunicatorMediaBegin);
                });
                registry.Add("media", Command.PlayerCommunicatorMediaChunk, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerCommunicatorMediaChunk(payload, out var chunk))
                        _owner.OnCommunicatorMediaChunk(player, chunk);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerCommunicatorMediaChunk);
                });
                registry.Add("media", Command.PlayerCommunicatorMediaEnd, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerCommunicatorMediaEnd(payload, out var end))
                        _owner.OnCommunicatorMediaEnd(player, end);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerCommunicatorMediaEnd);
                });
                registry.Add("media", Command.PlayerCommunicatorMediaState, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerCommunicatorMediaState(payload, out var state))
                        _owner.OnCommunicatorMediaState(player, state);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerCommunicatorMediaState);
                });
            }
        }
    }
}
