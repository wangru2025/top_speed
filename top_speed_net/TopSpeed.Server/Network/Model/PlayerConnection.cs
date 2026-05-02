using System;
using System.Collections.Generic;
using System.Net;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class PlayerConnection
    {
        public PlayerConnection(IPEndPoint endPoint, uint id, ulong resumeToken)
        {
            EndPoint = endPoint;
            Id = id;
            ResumeToken = resumeToken;
            Frequency = ProtocolConstants.DefaultFrequency;
            State = PlayerState.NotReady;
            Name = string.Empty;
            LastSeenUtc = DateTime.UtcNow;
            WidthM = 1.8f;
            LengthM = 4.5f;
            MassKg = 1500f;
            Handshake = HandshakeState.Pending;
            NegotiatedProtocol = ProtocolProfile.ServerSupported.MaxSupported;
            RadioVolumePercent = 100;
        }

        public IPEndPoint EndPoint { get; private set; }
        public uint Id { get; }
        public uint? RoomId { get; set; }
        public byte PlayerNumber { get; set; }
        public CarType Car { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public ushort Speed { get; set; }
        public int Frequency { get; set; }
        public PlayerState State { get; set; }
        public string Name { get; set; }
        public bool ServerPresenceAnnounced { get; set; }
        public bool EngineRunning { get; set; }
        public bool Braking { get; set; }
        public bool Horning { get; set; }
        public bool Backfiring { get; set; }
        public bool MediaLoaded { get; set; }
        public bool MediaPlaying { get; set; }
        public uint MediaId { get; set; }
        public byte RadioVolumePercent { get; set; }
        public InMedia? IncomingMedia { get; set; }
        public LiveState? Live { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public DateTime? SuspendedUtc { get; private set; }
        public ConnectionLifecycleState LifecycleState { get; private set; } = ConnectionLifecycleState.Connected;
        public bool Connected => LifecycleState == ConnectionLifecycleState.Connected || LifecycleState == ConnectionLifecycleState.Resumed;
        public ulong ResumeToken { get; private set; }
        public float WidthM { get; set; }
        public float LengthM { get; set; }
        public float MassKg { get; set; }
        public HandshakeState Handshake { get; set; }
        public ProtocolVer NegotiatedProtocol { get; set; }
        public ProtocolRange? ClientSupportedRange { get; set; }
        public ProtocolVer ClientVersion { get; set; }

        public void Rebind(IPEndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            LifecycleState = ConnectionLifecycleState.Resumed;
            SuspendedUtc = null;
            LastSeenUtc = DateTime.UtcNow;
        }

        public void MarkSuspended()
        {
            LifecycleState = ConnectionLifecycleState.Suspended;
            SuspendedUtc = DateTime.UtcNow;
            IncomingMedia = null;
        }

        public void MarkReconnecting()
        {
            LifecycleState = ConnectionLifecycleState.Reconnecting;
        }

        public void MarkConnected()
        {
            LifecycleState = ConnectionLifecycleState.Connected;
            SuspendedUtc = null;
            LastSeenUtc = DateTime.UtcNow;
        }

        public void MarkExpired()
        {
            LifecycleState = ConnectionLifecycleState.Expired;
        }

        public void MarkClosed()
        {
            LifecycleState = ConnectionLifecycleState.Closed;
            ResumeToken = 0;
            SuspendedUtc = null;
            IncomingMedia = null;
        }

        public PacketPlayerData ToPacket()
        {
            return new PacketPlayerData
            {
                PlayerId = Id,
                PlayerNumber = PlayerNumber,
                Car = Car,
                RaceData = new PlayerRaceData
                {
                    PositionX = PositionX,
                    PositionY = PositionY,
                    Speed = Speed,
                    Frequency = Frequency
                },
                State = State,
                EngineRunning = EngineRunning,
                Braking = Braking,
                Horning = Horning,
                Backfiring = Backfiring,
                MediaLoaded = MediaLoaded,
                MediaPlaying = MediaPlaying,
                MediaId = MediaId,
                RadioVolumePercent = RadioVolumePercent
            };
        }
    }

}
