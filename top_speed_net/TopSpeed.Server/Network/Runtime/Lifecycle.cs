using System;
using System.Linq;
using System.Net;
using System.Threading;
using LiteNetLib;
using System.Net.Sockets;
using TopSpeed.Localization;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static readonly string SnapshotServerName = LocalizationService.Mark("TopSpeed Server");

        public void Start()
        {
            ResetStreamTxMetrics();
            _transport.Start(_config.Port);
            _logger.Info(LocalizationService.Mark("Race server started."));
        }

        public void Stop()
        {
            _transport.Stop();

            lock (_lock)
            {
                DrainCommandQueueUnsafe();
                _rooms.Clear();
                _players.Clear();
                _endpointIndex.Clear();
                _endpointEpochIndex.Clear();
                ResetStreamTxMetrics();
            }

            _logger.Info(LocalizationService.Mark("Race server stopped."));
        }

        public void Update(float deltaSeconds)
        {
            _runtime.Update(deltaSeconds);
        }

        private void CleanupConnections()
        {
            _session.CleanupExpiredConnections();
        }

        private void OnPeerDisconnected(
            IPEndPoint endpoint,
            TransportDisconnectClassification disconnectClassification,
            DisconnectReason transportDisconnectReason,
            SocketError transportSocketError)
        {
            if (endpoint == null)
                return;

            var key = endpoint.ToString();
            _endpointEpochIndex.TryGetValue(key, out var endpointEpoch);
            _commandBus.EnqueuePeerDisconnected(
                endpoint,
                endpointEpoch,
                disconnectClassification,
                transportDisconnectReason,
                transportSocketError);
        }

        private void OnNetworkError(IPEndPoint endpoint, SocketError socketError)
        {
            Interlocked.Increment(ref _transportNetworkErrorCount);
            _logger.Warning(LocalizationService.Format(
                LocalizationService.Mark("LiteNetLib network error: endpoint={0}, socketError={1}."),
                endpoint,
                socketError));
        }

        private void OnPeerLatencyUpdated(IPEndPoint endpoint, int latency)
        {
            if (latency < 0)
                latency = 0;

            Interlocked.Exchange(ref _transportLastLatencyMs, latency);
            Interlocked.Add(ref _transportLatencyTotalMs, latency);
            Interlocked.Increment(ref _transportLatencySampleCount);

            while (true)
            {
                var currentMax = Volatile.Read(ref _transportMaxLatencyMs);
                if (latency <= currentMax)
                    break;

                if (Interlocked.CompareExchange(ref _transportMaxLatencyMs, latency, currentMax) == currentMax)
                    break;
            }
        }

        private void OnPeerAddressChanged(IPEndPoint previousAddress, IPEndPoint currentAddress)
        {
            if (previousAddress == null || currentAddress == null)
                return;

            lock (_lock)
            {
                var previousKey = previousAddress.ToString();
                if (!_endpointIndex.TryGetValue(previousKey, out var playerId))
                    return;

                if (!_players.TryGetValue(playerId, out var player))
                    return;

                _endpointIndex.Remove(previousKey);
                _endpointEpochIndex.TryRemove(previousKey, out _);
                player.UpdateTransportEndPoint(currentAddress);
                _endpointIndex[currentAddress.ToString()] = playerId;
                _endpointEpochIndex[currentAddress.ToString()] = player.ConnectionEpoch;
                _transportPeerAddressChangedCount++;
            }

            _logger.Info(LocalizationService.Format(
                LocalizationService.Mark("LiteNetLib peer address changed: old={0}, new={1}."),
                previousAddress,
                currentAddress));
        }

        private void RemoveConnection(
            PlayerConnection player,
            bool notifyRoom,
            bool sendDisconnectPacket,
            string reason,
            string? disconnectMessage = null,
            bool announcePresenceDisconnect = true)
        {
            _session.RemoveConnection(player, notifyRoom, sendDisconnectPacket, reason, disconnectMessage, announcePresenceDisconnect);
        }

        private static string BuildDisconnectMessage(string reason)
        {
            return Session.BuildDisconnectMessage(reason);
        }

        public ServerSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                var raceStarted = _rooms.Values.Any(r => r.RaceStarted);
                var trackSelected = _rooms.Values.Any(r => r.TrackSelected);
                var trackName = _rooms.Count == 1
                    ? _rooms.Values.First().TrackName
                    : (_rooms.Count > 1 ? LocalizationService.Mark("multiple") : string.Empty);
                return new ServerSnapshot(SnapshotServerName, _config.Port, _config.MaxPlayers, _players.Count, raceStarted, trackSelected, trackName);
            }
        }

        public void Dispose()
        {
            _transport.Dispose();
        }

        private void DrainCommandQueueUnsafe()
        {
            while (_commandBus.TryDequeue(out var command))
            {
                try
                {
                    switch (command.Kind)
                    {
                        case ServerCommandKind.PacketReceived:
                            if (command.EndPoint != null && command.Payload != null)
                                _session.HandlePacket(command.EndPoint, command.Payload, command.Sequence, command.EndpointEpoch);
                            break;

                        case ServerCommandKind.PeerDisconnected:
                            if (command.EndPoint != null)
                            {
                                _session.HandlePeerDisconnected(
                                    command.EndPoint,
                                    command.EndpointEpoch,
                                    command.DisconnectClassification,
                                    command.TransportDisconnectReason,
                                    command.TransportSocketError);
                            }
                            break;

                        case ServerCommandKind.ExecuteAction:
                            command.Action?.Invoke();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    command.SetError(ex);
                    _logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Server command failed: kind={0}, sequence={1}, reason={2}."),
                        command.Kind,
                        command.Sequence,
                        ex.Message));
                }
                finally
                {
                    command.Completion?.Set();
                }
            }
        }

        private void EnsureServerLoopThreadUnsafe()
        {
            if (_serverLoopThreadId != 0)
                return;

            _serverLoopThreadId = Environment.CurrentManagedThreadId;
        }

        private bool IsServerLoopThread()
        {
            var loopThread = Volatile.Read(ref _serverLoopThreadId);
            if (loopThread == 0)
                return false;

            return loopThread == Environment.CurrentManagedThreadId;
        }

        private void ExecuteOnServerLoop(Action action, bool waitForCompletion)
        {
            if (action == null)
                return;

            if (IsServerLoopThread())
            {
                action();
                return;
            }

            if (waitForCompletion && Volatile.Read(ref _serverLoopThreadId) == 0)
            {
                lock (_lock)
                {
                    EnsureServerLoopThreadUnsafe();
                    action();
                }

                return;
            }

            var completion = _commandBus.EnqueueAction(action, waitForCompletion);
            if (completion == null)
                return;

            completion.Wait();
            completion.Dispose();
        }
    }
}

