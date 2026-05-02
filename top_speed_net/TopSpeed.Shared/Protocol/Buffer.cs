using System;
using System.Text;

namespace TopSpeed.Protocol
{
    public struct PacketReader
    {
        private readonly byte[] _data;
        private int _offset;

        public PacketReader(byte[] data)
        {
            _data = data;
            _offset = 0;
        }

        public byte ReadByte() => _data[_offset++];
        public bool ReadBool() => ReadByte() != 0;

        public ushort ReadUInt16()
        {
            var value = (ushort)(_data[_offset] | (_data[_offset + 1] << 8));
            _offset += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            var value = (uint)(_data[_offset]
                | (_data[_offset + 1] << 8)
                | (_data[_offset + 2] << 16)
                | (_data[_offset + 3] << 24));
            _offset += 4;
            return value;
        }

        public ulong ReadUInt64()
        {
            var lo = ReadUInt32();
            var hi = ReadUInt32();
            return lo | ((ulong)hi << 32);
        }

        public int ReadInt32()
        {
            var value = _data[_offset]
                | (_data[_offset + 1] << 8)
                | (_data[_offset + 2] << 16)
                | (_data[_offset + 3] << 24);
            _offset += 4;
            return value;
        }

        public float ReadSingle()
        {
            var value = BitConverter.ToSingle(_data, _offset);
            _offset += 4;
            return value;
        }

        public string ReadFixedString(int length)
        {
            var value = Encoding.UTF8.GetString(_data, _offset, length);
            _offset += length;
            var nullIndex = value.IndexOf('\0');
            return nullIndex >= 0 ? value.Substring(0, nullIndex) : value.Trim();
        }

        public string ReadString16()
        {
            var length = ReadUInt16();
            if (length == 0)
                return string.Empty;

            var value = Encoding.UTF8.GetString(_data, _offset, length);
            _offset += length;
            return value;
        }
    }

    public struct PacketWriter
    {
        private readonly byte[] _buffer;
        private int _offset;

        public PacketWriter(byte[] buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public void WriteByte(byte value) => _buffer[_offset++] = value;
        public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

        public void WriteUInt16(ushort value)
        {
            _buffer[_offset++] = (byte)(value & 0xFF);
            _buffer[_offset++] = (byte)(value >> 8);
        }

        public void WriteUInt32(uint value)
        {
            _buffer[_offset++] = (byte)(value & 0xFF);
            _buffer[_offset++] = (byte)((value >> 8) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 16) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 24) & 0xFF);
        }

        public void WriteUInt64(ulong value)
        {
            WriteUInt32((uint)(value & 0xFFFFFFFF));
            WriteUInt32((uint)(value >> 32));
        }

        public void WriteInt32(int value)
        {
            _buffer[_offset++] = (byte)(value & 0xFF);
            _buffer[_offset++] = (byte)((value >> 8) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 16) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 24) & 0xFF);
        }

        public void WriteSingle(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, _buffer, _offset, 4);
            _offset += 4;
        }

        public void WriteFixedString(string value, int length)
        {
            if (length <= 0)
                return;

            var text = value ?? string.Empty;
            var bytes = Encoding.UTF8.GetBytes(text);
            var count = Math.Min(length, bytes.Length);
            if (count == bytes.Length)
            {
                Array.Copy(bytes, 0, _buffer, _offset, count);
            }
            else
            {
                var chars = text.ToCharArray();
                var encoder = Encoding.UTF8.GetEncoder();
                encoder.Convert(chars, 0, chars.Length, _buffer, _offset, length, true, out _, out var bytesUsed, out _);
                count = bytesUsed;
            }

            for (var i = count; i < length; i++)
                _buffer[_offset + i] = 0;
            _offset += length;
        }

        public void WriteString16(string value)
        {
            var text = value ?? string.Empty;
            var length = MeasureString16(text);
            WriteUInt16((ushort)length);
            if (length == 0)
                return;

            var bytes = Encoding.UTF8.GetBytes(text);
            Array.Copy(bytes, 0, _buffer, _offset, length);
            _offset += length;
        }

        public static int MeasureString16(string value)
        {
            var text = value ?? string.Empty;
            return Encoding.UTF8.GetByteCount(text);
        }
    }
}
