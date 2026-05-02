using System;
using System.IO;
using System.Threading;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Session
{
    internal sealed class Media
    {
        private const int ChunkSize = ProtocolConstants.MaxMediaChunkBytes;
        private readonly Sender _sender;
        private int _nextTransferId;

        public Media(Sender sender)
        {
            _sender = sender;
        }

        public bool TrySendBuffered(uint playerId, byte playerNumber, uint mediaId, string filePath)
        {
            if (!TryValidateInput(mediaId, filePath))
                return false;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(filePath);
            }
            catch
            {
                return false;
            }

            if (!TryValidateLength(bytes.Length))
                return false;

            var extension = NormalizeExtension(filePath);
            var transferId = NextTransferId();
            if (!_sender.TrySend(
                ClientPacketSerializer.WritePlayerMediaBegin(playerId, playerNumber, mediaId, transferId, (uint)bytes.Length, extension),
                PacketStream.Media))
            {
                return false;
            }

            var chunkIndex = 0;
            var offset = 0;
            while (offset < bytes.Length)
            {
                var length = Math.Min(ChunkSize, bytes.Length - offset);
                var chunk = new byte[length];
                Buffer.BlockCopy(bytes, offset, chunk, 0, length);
                if (!_sender.TrySend(
                    ClientPacketSerializer.WritePlayerMediaChunk(playerId, playerNumber, mediaId, transferId, (ushort)chunkIndex, chunk),
                    PacketStream.Media))
                {
                    return false;
                }
                chunkIndex++;
                offset += length;
            }

            return _sender.TrySend(ClientPacketSerializer.WritePlayerMediaEnd(playerId, playerNumber, mediaId, transferId), PacketStream.Media);
        }

        public bool TrySendStreamed(uint playerId, byte playerNumber, uint mediaId, string filePath)
        {
            if (!TryValidateInput(mediaId, filePath))
                return false;

            FileStream? stream = null;
            try
            {
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (!TryValidateLength(stream.Length))
                    return false;

                var extension = NormalizeExtension(filePath);
                var transferId = NextTransferId();
                if (!_sender.TrySend(
                    ClientPacketSerializer.WritePlayerMediaBegin(playerId, playerNumber, mediaId, transferId, (uint)stream.Length, extension),
                    PacketStream.Media))
                {
                    return false;
                }

                var chunkIndex = 0;
                var buffer = new byte[ChunkSize];
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    var chunk = new byte[read];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, read);
                    if (!_sender.TrySend(
                        ClientPacketSerializer.WritePlayerMediaChunk(playerId, playerNumber, mediaId, transferId, (ushort)chunkIndex, chunk),
                        PacketStream.Media))
                    {
                        return false;
                    }
                    chunkIndex++;
                }

                return _sender.TrySend(ClientPacketSerializer.WritePlayerMediaEnd(playerId, playerNumber, mediaId, transferId), PacketStream.Media);
            }
            catch
            {
                return false;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private static bool TryValidateInput(uint mediaId, string filePath)
        {
            if (mediaId == 0 || string.IsNullOrWhiteSpace(filePath))
                return false;
            return File.Exists(filePath);
        }

        private static bool TryValidateLength(long length)
        {
            return length > 0 && length <= ProtocolConstants.MaxMediaBytes;
        }

        private static string NormalizeExtension(string filePath)
        {
            var extension = Path.GetExtension(filePath).Trim().TrimStart('.');
            if (extension.Length > ProtocolConstants.MaxMediaFileExtensionLength)
                extension = extension.Substring(0, ProtocolConstants.MaxMediaFileExtensionLength);
            return extension;
        }

        private uint NextTransferId()
        {
            var next = (uint)Interlocked.Increment(ref _nextTransferId);
            return next == 0 ? 1u : next;
        }
    }
}

