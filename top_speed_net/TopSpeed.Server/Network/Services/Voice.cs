using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed class Voice : IVoiceService
        {
            private readonly RaceServer _owner;

            public Voice(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void RegisterPackets(ServerPktReg registry)
            {
                registry.Add("voice", Command.PlayerVoiceStart, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerVoiceStart(payload, out var start))
                        _owner.OnVoiceStart(player, start);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerVoiceStart);
                });
                registry.Add("voice", Command.PlayerVoiceFrame, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerVoiceFrame(payload, out var frame))
                        _owner.OnVoiceFrame(player, frame);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerVoiceFrame);
                });
                registry.Add("voice", Command.PlayerVoiceStop, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadPlayerVoiceStop(payload, out var stop))
                        _owner.OnVoiceStop(player, stop);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerVoiceStop);
                });
            }
        }
    }
}
