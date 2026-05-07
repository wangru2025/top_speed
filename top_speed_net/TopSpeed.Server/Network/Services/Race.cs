using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Race : IRaceService
        {
            private readonly RaceServer _owner;

            public Race(RaceServer owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public void RegisterPackets(ServerPktReg registry)
            {
                registry.Add("race", Command.PlayerState, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRacePlayerState(payload, out var state))
                        HandlePlayerState(player, state);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerState);
                });
                registry.Add("race", Command.PlayerDataToServer, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRacePlayerData(payload, out var data))
                        HandlePlayerData(player, data);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerDataToServer);
                });
                registry.Add("race", Command.PlayerStarted, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRacePlayer(payload, out var started))
                        HandlePlayerStarted(player, started);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerStarted);
                });
                registry.Add("race", Command.PlayerCrashed, (player, payload, endPoint) =>
                {
                    if (PacketSerializer.TryReadRacePlayer(payload, out var crashed))
                        HandlePlayerCrashed(player, crashed);
                    else
                        _owner.PacketFail(endPoint, Command.PlayerCrashed);
                });
            }
        }
    }
}
