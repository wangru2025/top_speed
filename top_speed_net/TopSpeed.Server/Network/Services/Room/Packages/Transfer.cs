using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void SendPackageToPlayer(PlayerConnection player, PackageRecord package)
        {
            if (player == null || package == null || package.Bytes == null || package.Bytes.Length == 0)
                return;

            SendStream(player, PacketSerializer.WriteTrackPackageTransferBegin(new PacketTrackPackageTransferBegin
            {
                TrackId = package.Ref.TrackId,
                Version = package.Ref.Version,
                Hash = package.Ref.Hash,
                TotalBytes = (uint)package.Bytes.Length
            }), PacketStream.Room);

            var chunkSize = ProtocolConstants.MaxTrackPackageChunkBytes;
            var chunkIndex = 0;
            var offset = 0;
            while (offset < package.Bytes.Length)
            {
                var length = Math.Min(chunkSize, package.Bytes.Length - offset);
                var chunk = new byte[length];
                Buffer.BlockCopy(package.Bytes, offset, chunk, 0, length);
                SendStream(player, PacketSerializer.WriteTrackPackageTransferChunk(new PacketTrackPackageTransferChunk
                {
                    Hash = package.Ref.Hash,
                    ChunkIndex = (ushort)chunkIndex,
                    Data = chunk
                }), PacketStream.Room);
                offset += length;
                chunkIndex++;
            }

            SendStream(player, PacketSerializer.WriteTrackPackageTransferEnd(new PacketTrackPackageTransferEnd
            {
                Hash = package.Ref.Hash
            }), PacketStream.Room);
        }
    }
}

