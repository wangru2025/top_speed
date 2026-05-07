using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using LiteNetLib;

namespace TopSpeed.Server.Network
{
    internal enum ServerCommandKind : byte
    {
        PacketReceived = 0,
        PeerDisconnected = 1,
        ExecuteAction = 2
    }

    internal sealed class ServerCommand
    {
        private ServerCommand(
            long sequence,
            ServerCommandKind kind,
            IPEndPoint? endPoint,
            byte[]? payload,
            uint endpointEpoch,
            TransportDisconnectClassification disconnectClassification,
            DisconnectReason transportDisconnectReason,
            SocketError transportSocketError,
            Action? action,
            ManualResetEventSlim? completion)
        {
            Sequence = sequence;
            Kind = kind;
            EndPoint = endPoint;
            Payload = payload;
            EndpointEpoch = endpointEpoch;
            DisconnectClassification = disconnectClassification;
            TransportDisconnectReason = transportDisconnectReason;
            TransportSocketError = transportSocketError;
            Action = action;
            Completion = completion;
        }

        public long Sequence { get; }
        public ServerCommandKind Kind { get; }
        public IPEndPoint? EndPoint { get; }
        public byte[]? Payload { get; }
        public uint EndpointEpoch { get; }
        public TransportDisconnectClassification DisconnectClassification { get; }
        public DisconnectReason TransportDisconnectReason { get; }
        public SocketError TransportSocketError { get; }
        public Action? Action { get; }
        public ManualResetEventSlim? Completion { get; }
        public Exception? Error { get; private set; }

        public static ServerCommand Packet(long sequence, IPEndPoint endPoint, byte[] payload, uint endpointEpoch)
        {
            return new ServerCommand(
                sequence,
                ServerCommandKind.PacketReceived,
                endPoint,
                payload,
                endpointEpoch,
                disconnectClassification: default,
                transportDisconnectReason: default,
                transportSocketError: default,
                action: null,
                completion: null);
        }

        public static ServerCommand PeerDisconnected(
            long sequence,
            IPEndPoint endPoint,
            uint endpointEpoch,
            TransportDisconnectClassification disconnectClassification,
            DisconnectReason transportDisconnectReason,
            SocketError transportSocketError)
        {
            return new ServerCommand(
                sequence,
                ServerCommandKind.PeerDisconnected,
                endPoint,
                payload: null,
                endpointEpoch,
                disconnectClassification,
                transportDisconnectReason,
                transportSocketError,
                action: null,
                completion: null);
        }

        public static ServerCommand ExecuteAction(long sequence, Action action, ManualResetEventSlim? completion)
        {
            return new ServerCommand(
                sequence,
                ServerCommandKind.ExecuteAction,
                endPoint: null,
                payload: null,
                endpointEpoch: 0,
                disconnectClassification: default,
                transportDisconnectReason: default,
                transportSocketError: default,
                action,
                completion);
        }

        public void SetError(Exception error)
        {
            Error = error;
        }
    }

    internal sealed class ServerCommandBus
    {
        private readonly ConcurrentQueue<ServerCommand> _queue = new ConcurrentQueue<ServerCommand>();
        private long _nextSequence;

        public void EnqueuePacket(IPEndPoint endPoint, byte[] payload, uint endpointEpoch)
        {
            var sequence = Interlocked.Increment(ref _nextSequence);
            _queue.Enqueue(ServerCommand.Packet(sequence, endPoint, payload, endpointEpoch));
        }

        public void EnqueuePeerDisconnected(
            IPEndPoint endPoint,
            uint endpointEpoch,
            TransportDisconnectClassification disconnectClassification,
            DisconnectReason transportDisconnectReason,
            SocketError transportSocketError)
        {
            var sequence = Interlocked.Increment(ref _nextSequence);
            _queue.Enqueue(ServerCommand.PeerDisconnected(
                sequence,
                endPoint,
                endpointEpoch,
                disconnectClassification,
                transportDisconnectReason,
                transportSocketError));
        }

        public ManualResetEventSlim? EnqueueAction(Action action, bool waitForCompletion)
        {
            var completion = waitForCompletion ? new ManualResetEventSlim(false) : null;
            var sequence = Interlocked.Increment(ref _nextSequence);
            _queue.Enqueue(ServerCommand.ExecuteAction(sequence, action, completion));
            return completion;
        }

        public bool TryDequeue(out ServerCommand command)
        {
            return _queue.TryDequeue(out command!);
        }
    }
}
