using System;
using System.Net;
using System.Security.Cryptography;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void RegisterCorePackets()
        {
            _pktReg.Add("core", Command.KeepAlive, (_, _, _) => { });
            _pktReg.Add("core", Command.Ping, (player, _, _) =>
            {
                SendStream(player, PacketSerializer.WriteGeneral(Command.Pong), PacketStream.Control);
            });
            _pktReg.Add("core", Command.PlayerHello, (player, payload, endPoint) =>
            {
                if (PacketSerializer.TryReadPlayerHello(payload, out var hello))
                    HandlePlayerHello(player, hello);
                else
                    PacketFail(endPoint, Command.PlayerHello);
            });
        }

        private void PacketFail(IPEndPoint endPoint, Command command)
        {
            _logger.Warning(LocalizationService.Format(
                LocalizationService.Mark("Failed to parse {0} packet from {1}."),
                command,
                endPoint));
        }

        private PlayerConnection? GetOrAddPlayer(IPEndPoint endpoint)
        {
            var key = endpoint.ToString();
            if (_endpointIndex.TryGetValue(key, out var id) && _players.TryGetValue(id, out var existing))
                return existing;

            if (_players.Count >= _config.MaxPlayers && !HasDisconnectedResumeCandidate())
            {
                SendStream(endpoint, PacketSerializer.WriteDisconnect(LocalizationService.Mark("This server is full.")), PacketStream.Control);
                _logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("Refused connection from {0}: server is full."),
                    endpoint));
                return null;
            }

            var playerId = _nextPlayerId++;
            var player = new PlayerConnection(endpoint, playerId, CreateResumeToken());
            _players[playerId] = player;
            _endpointIndex[key] = playerId;

            _logger.Debug(LocalizationService.Format(
                LocalizationService.Mark("Connection pending protocol negotiation: playerId={0}, endpoint={1}."),
                player.Id,
                endpoint));
            return player;
        }

        private bool HasDisconnectedResumeCandidate()
        {
            foreach (var player in _players.Values)
            {
                if (ConnectionRecoveryRules.IsRecoverableState(player.LifecycleState) && player.SuspendedUtc.HasValue)
                    return true;
            }

            return false;
        }

        private static ulong CreateResumeToken()
        {
            Span<byte> bytes = stackalloc byte[8];
            ulong token;
            do
            {
                RandomNumberGenerator.Fill(bytes);
                token = BitConverter.ToUInt64(bytes);
            }
            while (token == 0);

            return token;
        }
    }
}
