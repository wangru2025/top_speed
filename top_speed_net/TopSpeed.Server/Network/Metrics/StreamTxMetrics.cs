using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal readonly struct StreamTxMetric
    {
        public StreamTxMetric(PacketStream stream, long packets, long bytes)
        {
            Stream = stream;
            Packets = packets;
            Bytes = bytes;
        }

        public PacketStream Stream { get; }
        public long Packets { get; }
        public long Bytes { get; }
    }

    internal sealed partial class RaceServer
    {
        private readonly long[] _streamTxPackets = new long[PacketStreams.Count];
        private readonly long[] _streamTxBytes = new long[PacketStreams.Count];

        internal StreamTxMetric[] GetStreamTxMetricsSnapshot()
        {
            lock (_lock)
            {
                return CopyStreamTxMetricsUnsafe();
            }
        }

        private StreamTxMetric[] CopyStreamTxMetricsUnsafe()
        {
            var result = new StreamTxMetric[PacketStreams.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new StreamTxMetric(
                    (PacketStream)i,
                    _streamTxPackets[i],
                    _streamTxBytes[i]);
            }

            return result;
        }

        private void TrackStreamSend(PacketStream stream, int payloadBytes)
        {
            var index = (int)stream;
            if (index < 0 || index >= PacketStreams.Count)
                return;

            _streamTxPackets[index]++;
            if (payloadBytes > 0)
                _streamTxBytes[index] += payloadBytes;
        }

        private void ResetStreamTxMetrics()
        {
            Array.Clear(_streamTxPackets, 0, _streamTxPackets.Length);
            Array.Clear(_streamTxBytes, 0, _streamTxBytes.Length);
        }
    }
}
