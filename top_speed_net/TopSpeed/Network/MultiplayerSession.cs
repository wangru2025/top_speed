using System;
using System.Collections.Concurrent;
using System.Net;
using LiteNetLib;
using TopSpeed.Network.Session;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerSession : IDisposable
    {
        private readonly NetManager _manager;
        private readonly IPEndPoint _serverEndPoint;
        private readonly ConcurrentQueue<IncomingPacket> _incoming;
        private readonly Sender _sender;
        private readonly Media _media;
        private readonly LiveSend _live;
        private readonly Loop _loop;
        private Action<IncomingPacket>? _packetSink;
        private byte _playerNumber;

        public MultiplayerSession(
            NetManager manager,
            NetPeer peer,
            IPEndPoint serverEndPoint,
            uint playerId,
            byte playerNumber,
            ulong resumeToken,
            string? motd,
            string? playerName,
            ConcurrentQueue<IncomingPacket> incoming)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _serverEndPoint = serverEndPoint ?? throw new ArgumentNullException(nameof(serverEndPoint));
            _incoming = incoming ?? throw new ArgumentNullException(nameof(incoming));
            _sender = new Sender(peer ?? throw new ArgumentNullException(nameof(peer)));
            _media = new Media(_sender);
            _live = new LiveSend(_sender);
            PlayerId = playerId;
            ResumeToken = resumeToken;
            _playerNumber = playerNumber;
            Motd = motd ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            _loop = new Loop(PollEventsSafe, DrainIncomingToSink, SendKeepAlive);
        }

        public IPAddress Address => _serverEndPoint.Address;
        public int Port => _serverEndPoint.Port;
        public uint PlayerId { get; }
        public ulong ResumeToken { get; }
        public byte PlayerNumber => _playerNumber;
        public string Motd { get; }
        public string PlayerName { get; }

        public void UpdatePlayerNumber(byte playerNumber)
        {
            _playerNumber = playerNumber;
        }

        public bool TryDequeuePacket(out IncomingPacket packet)
        {
            return _incoming.TryDequeue(out packet);
        }

        public void SetPacketSink(Action<IncomingPacket>? packetSink)
        {
            _packetSink = packetSink;
            if (packetSink != null)
                DrainIncomingToSink();
        }

        public void Dispose()
        {
            _live.Reset();
            _loop.Dispose();
            _manager.Stop();
        }
    }

    internal readonly struct IncomingPacket
    {
        public IncomingPacket(Command command, byte[] payload)
            : this(command, payload, 0)
        {
        }

        public IncomingPacket(Command command, byte[] payload, long receivedUtcTicks)
        {
            Command = command;
            Payload = payload;
            ReceivedUtcTicks = receivedUtcTicks;
        }

        public Command Command { get; }
        public byte[] Payload { get; }
        public long ReceivedUtcTicks { get; }
    }
}

