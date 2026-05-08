using System.Net;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void OnPacket(IPEndPoint endPoint, byte[] payload)
        {
            if (endPoint == null || payload == null || payload.Length == 0)
                return;

            var key = endPoint.ToString();
            _endpointEpochIndex.TryGetValue(key, out var endpointEpoch);
            _commandBus.EnqueuePacket(endPoint, payload, endpointEpoch);
        }

        private void RegisterPackets()
        {
            RegisterCorePackets();
            RegisterRacePackets();
            RegisterMediaPackets();
            RegisterLivePackets();
            RegisterRoomPackets();
            RegisterChatPackets();
        }

    }
}
