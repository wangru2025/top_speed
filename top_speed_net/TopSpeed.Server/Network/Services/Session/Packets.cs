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
            public void HandlePacket(IPEndPoint endPoint, byte[] payload)
            {
                if (!PacketSerializer.TryReadHeader(payload, out var header))
                {
                    _owner._logger.Warning(LocalizationService.Format(
                        LocalizationService.Mark("Dropped packet with invalid header from {0}."),
                        endPoint));
                    return;
                }

                if (header.Version != ProtocolConstants.Version)
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Dropped packet with protocol version mismatch from {0}: received={1}, expected={2}."),
                        endPoint,
                        header.Version,
                        ProtocolConstants.Version));
                    return;
                }

                lock (_owner._lock)
                {
                    var player = _owner.GetOrAddPlayer(endPoint);
                    if (player == null)
                        return;

                    if (player.Connected)
                        player.LastSeenUtc = DateTime.UtcNow;
                    if (_owner.HandlePendingHandshake(player, header.Command, payload, endPoint))
                        return;

                    if (!_owner._pktReg.TryDispatch(header.Command, player, payload, endPoint))
                    {
                        _owner._logger.Warning(LocalizationService.Format(
                            LocalizationService.Mark("Ignoring unknown packet command {0} from {1}."),
                            (byte)header.Command,
                            endPoint));
                    }
                }
            }
        }
    }
}
