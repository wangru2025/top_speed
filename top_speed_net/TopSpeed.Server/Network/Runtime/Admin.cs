using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Server.Config;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        public void SetMotd(string motd)
        {
            var normalized = motd ?? string.Empty;
            ExecuteOnServerLoop(() => _config.Motd = normalized, waitForCompletion: true);
        }

        public void SetMaxPlayers(int maxPlayers)
        {
            ExecuteOnServerLoop(() => _config.MaxPlayers = maxPlayers, waitForCompletion: true);
        }

        public void SetModerationSettings(ServerModerationSettings moderation)
        {
            if (moderation == null)
                return;

            var cloned = moderation.Clone();
            ExecuteOnServerLoop(() => _config.Moderation = cloned, waitForCompletion: true);
        }

        public void SetFeatureSettings(ServerFeaturesSettings features)
        {
            if (features == null)
                return;

            var cloned = features.Clone();
            ExecuteOnServerLoop(() => _config.Features = cloned, waitForCompletion: true);
        }

        public ServerPlayerInfo[] GetPlayersSnapshot()
        {
            lock (_lock)
            {
                var players = _players.Values
                    .OrderBy(player => player.PlayerNumber)
                    .ToArray();
                var result = new List<ServerPlayerInfo>(players.Length);
                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    result.Add(new ServerPlayerInfo(GetPlayerDisplayName(player), player.ClientVersion));
                }

                return result.ToArray();
            }
        }

        public int ShutdownByHost(string announcementMessage)
        {
            var message = string.IsNullOrWhiteSpace(announcementMessage)
                ? LocalizationService.Mark("The server will be shut down immediately by the host.")
                : announcementMessage.Trim();

            var removedCount = 0;
            ExecuteOnServerLoop(() =>
            {
                var players = _players.Values
                    .OrderBy(player => player.PlayerNumber)
                    .ToArray();

                for (var i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (!_players.ContainsKey(player.Id))
                        continue;

                    RemoveConnection(
                        player,
                        notifyRoom: false,
                        sendDisconnectPacket: true,
                        reason: "host_shutdown",
                        disconnectMessage: message,
                        announcePresenceDisconnect: false);
                }

                removedCount = players.Length;
            }, waitForCompletion: true);

            return removedCount;
        }

        public string GetDiagnosticsSummary()
        {
            lock (_lock)
            {
                var latencySamples = _transportLatencySampleCount;
                var latencyAvg = latencySamples <= 0
                    ? 0
                    : (int)Math.Round((double)_transportLatencyTotalMs / latencySamples);

                return LocalizationService.Format(
                    LocalizationService.Mark("authoritative players={0}, rooms={1}, stale_epoch_drops={2}, replay_gaps={3}, epoch_rejects={4}, heartbeat_suspicions={5}, start_blocked_insufficient_active={6}, start_blocked_missing_ready={7}, start_blocked_track_not_ready={8}, transport_network_errors={9}, transport_latency_last_ms={10}, transport_latency_avg_ms={11}, transport_latency_max_ms={12}, transport_peer_address_changes={13}"),
                    _players.Count,
                    _rooms.Count,
                    _droppedPacketsStaleEpoch,
                    _replayGapCount,
                    _epochRejectCount,
                    _heartbeatSuspicionCount,
                    _startBarrierBlockedInsufficientActive,
                    _startBarrierBlockedMissingReady,
                    _startBarrierBlockedTrackNotReady,
                    _transportNetworkErrorCount,
                    _transportLastLatencyMs,
                    latencyAvg,
                    _transportMaxLatencyMs,
                    _transportPeerAddressChangedCount);
            }
        }

        private static string GetPlayerDisplayName(PlayerConnection player)
        {
            if (!string.IsNullOrWhiteSpace(player.Name))
                return player.Name;
            return LocalizationService.Format(LocalizationService.Mark("Player {0}"), player.PlayerNumber + 1);
        }
    }
}
