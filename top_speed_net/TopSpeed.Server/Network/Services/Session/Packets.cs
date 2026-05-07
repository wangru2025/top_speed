using System;
using System.Net;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Session
        {
            public void HandlePacket(IPEndPoint endPoint, byte[] payload, long commandSequence, uint endpointEpoch)
            {
                if (!PacketSerializer.TryReadHeader(payload, out var header))
                {
                    _owner._droppedPacketsInvalidHeader++;
                    _owner._logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Dropped packet with invalid header from {0}."),
                        endPoint));
                    return;
                }

                if (header.Version != ProtocolConstants.Version)
                {
                    _owner._droppedPacketsVersionMismatch++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Dropped packet with protocol version mismatch from {0}: received={1}, expected={2}."),
                        endPoint,
                        header.Version,
                        ProtocolConstants.Version));
                    return;
                }

                var endpointKey = endPoint.ToString();
                PlayerConnection? player = null;
                var hasKnownEndpoint = _owner._endpointIndex.TryGetValue(endpointKey, out var mappedId)
                                       && _owner._players.TryGetValue(mappedId, out player);
                if (endpointEpoch != 0)
                {
                    if (!_owner._endpointEpochIndex.TryGetValue(endpointKey, out var expectedEpoch)
                        || expectedEpoch != endpointEpoch)
                    {
                        _owner._droppedPacketsStaleEpoch++;
                        _owner._epochRejectCount++;
                        _owner._logger.Debug(LocalizationService.Format(
                            LocalizationService.Mark("Dropped stale packet epoch from {0}: command={1}, sequence={2}, expectedEpoch={3}, commandEpoch={4}."),
                            endPoint,
                            header.Command,
                            commandSequence,
                            expectedEpoch,
                            endpointEpoch));
                        return;
                    }
                }
                else if (hasKnownEndpoint && player != null && player.ConnectionEpoch > 1)
                {
                    _owner._droppedPacketsStaleEpoch++;
                    _owner._epochRejectCount++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Dropped stale packet from prior epoch: endpoint={0}, command={1}, sequence={2}, activeEpoch={3}."),
                        endPoint,
                        header.Command,
                        commandSequence,
                        player.ConnectionEpoch));
                    return;
                }

                if (!hasKnownEndpoint)
                {
                    if (header.Command != Command.ProtocolHello)
                    {
                        _owner._droppedPacketsUnknownCommand++;
                        _owner._logger.Debug(LocalizationService.Format(
                            LocalizationService.Mark("Dropped pre-auth packet from unknown endpoint {0}: command={1}, sequence={2}."),
                            endPoint,
                            header.Command,
                            commandSequence));
                        return;
                    }

                    player = _owner.GetOrAddPlayer(endPoint);
                    if (player == null)
                        return;
                }

                if (player == null)
                    return;

                if (_owner.HandlePendingHandshake(player, header.Command, payload, endPoint))
                    return;

                if (!_owner._pktReg.TryDispatch(header.Command, player, payload, endPoint))
                {
                    _owner._droppedPacketsUnknownCommand++;
                    _owner._logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Ignoring unknown packet command {0} from {1}."),
                        (byte)header.Command,
                        endPoint));
                }
            }
        }
    }
}
