using LiteNetLib;
using System.Net;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private static DeliveryMethod ToDelivery(PacketDeliveryKind kind)
        {
            return kind switch
            {
                PacketDeliveryKind.Unreliable => DeliveryMethod.Unreliable,
                PacketDeliveryKind.Sequenced => DeliveryMethod.Sequenced,
                _ => DeliveryMethod.ReliableOrdered
            };
        }

        private void SendStream(PlayerConnection player, byte[] payload, PacketStream stream)
        {
            if (player == null || payload == null)
                return;

            var spec = PacketStreams.Get(stream);
            TrackStreamSend(stream, payload.Length);
            _transport.Send(player.EndPoint, payload, ToDelivery(spec.Delivery), spec.Channel);
        }

        private void SendStream(PlayerConnection player, byte[] payload, PacketStream stream, PacketDeliveryKind deliveryOverride)
        {
            if (player == null || payload == null)
                return;

            var spec = PacketStreams.Get(stream);
            TrackStreamSend(stream, payload.Length);
            _transport.Send(player.EndPoint, payload, ToDelivery(deliveryOverride), spec.Channel);
        }

        private void SendStream(IPEndPoint endpoint, byte[] payload, PacketStream stream)
        {
            if (endpoint == null || payload == null)
                return;

            var spec = PacketStreams.Get(stream);
            TrackStreamSend(stream, payload.Length);
            _transport.Send(endpoint, payload, ToDelivery(spec.Delivery), spec.Channel);
        }

        private void SendStream(IPEndPoint endpoint, byte[] payload, PacketStream stream, PacketDeliveryKind deliveryOverride)
        {
            if (endpoint == null || payload == null)
                return;

            var spec = PacketStreams.Get(stream);
            TrackStreamSend(stream, payload.Length);
            _transport.Send(endpoint, payload, ToDelivery(deliveryOverride), spec.Channel);
        }

    }
}
