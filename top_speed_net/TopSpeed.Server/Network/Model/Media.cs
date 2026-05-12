using System;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class MediaBlob
    {
        public uint MediaId { get; set; }
        public uint TransferId { get; set; }
        public MediaTransferState State { get; set; } = MediaTransferState.Complete;
        public string Extension { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    internal sealed class InMedia
    {
        public uint MediaId { get; set; }
        public uint TransferId { get; set; }
        public MediaTransferState State { get; set; } = MediaTransferState.Idle;
        public string Extension { get; set; } = string.Empty;
        public uint TotalBytes { get; set; }
        public ushort NextChunk { get; set; }
        public bool BufferEnabled { get; set; }
        public byte[] Buffer { get; set; } = Array.Empty<byte>();
        public int Offset { get; set; }

        public bool IsComplete => TotalBytes > 0 && Offset >= (int)TotalBytes;
    }

    internal sealed class CommunicatorMediaState
    {
        public uint MediaId { get; set; }
        public ushort FrequencyTenths { get; set; }
        public bool MediaLoaded { get; set; }
        public bool MediaPlaying { get; set; }
        public byte VolumePercent { get; set; } = 100;
    }

}
