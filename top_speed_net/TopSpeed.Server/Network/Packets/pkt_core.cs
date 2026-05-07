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
            _pktReg.Add("core", Command.KeepAlive, (player, _, _) =>
            {
                player.LastHeartbeatUtc = DateTime.UtcNow;
            });
            _pktReg.Add("core", Command.Ping, (player, _, _) =>
            {
                player.LastHeartbeatUtc = DateTime.UtcNow;
                SendStream(player, PacketSerializer.WriteGeneral(Command.Pong), PacketStream.Control);
            });
            _pktReg.Add("core", Command.ClientHeartbeat, (player, payload, endPoint) =>
            {
                if (!PacketSerializer.TryReadClientHeartbeat(payload, out var heartbeat))
                {
                    PacketFail(endPoint, Command.ClientHeartbeat);
                    return;
                }

                if (heartbeat.PlayerId != 0 && heartbeat.PlayerId != player.Id)
                {
                    _logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Ignored client heartbeat with mismatched player id from {0}: expected={1}, received={2}."),
                        endPoint,
                        player.Id,
                        heartbeat.PlayerId));
                    return;
                }

                if (heartbeat.SessionId != 0 && heartbeat.SessionId != player.ResumeToken)
                {
                    _logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Ignored client heartbeat with mismatched session id from {0}: expected={1}, received={2}."),
                        endPoint,
                        player.ResumeToken,
                        heartbeat.SessionId));
                    return;
                }

                player.LastClientHeartbeatTick = heartbeat.ClientTick;
                player.LastClientObservedServerTick = heartbeat.LastReceivedServerTick;
                if (player.Handshake == HandshakeState.Complete)
                    player.MarkActive();

                var response = PacketSerializer.WriteServerHeartbeat(new PacketServerHeartbeat
                {
                    ServerTick = _simulationTick,
                    LastReceivedClientTick = heartbeat.ClientTick
                });
                SendStream(player, response, PacketStream.Control, PacketDeliveryKind.Unreliable);
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
            {
                _endpointEpochIndex[key] = existing.ConnectionEpoch;
                return existing;
            }

            _endpointIndex.Remove(key);
            _endpointEpochIndex.TryRemove(key, out _);

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
            _endpointEpochIndex[key] = player.ConnectionEpoch;

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
