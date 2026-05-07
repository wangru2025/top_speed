using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;

namespace TopSpeed.Server.Network
{
    internal interface ISessionService
    {
        void HandlePacket(IPEndPoint endPoint, byte[] payload, long commandSequence, uint endpointEpoch);

        void HandlePeerDisconnected(
            IPEndPoint endpoint,
            uint endpointEpoch,
            TransportDisconnectClassification disconnectClassification,
            DisconnectReason transportDisconnectReason,
            SocketError transportSocketError);

        void CleanupExpiredConnections();
        PlayerConnection? ResolveResume(PlayerConnection pending, uint resumePlayerId, ulong resumeToken);
        void SendInitialConnectionState(PlayerConnection player);

        void RemoveConnection(
            PlayerConnection player,
            bool notifyRoom,
            bool sendDisconnectPacket,
            string reason,
            string? disconnectMessage = null,
            bool announcePresenceDisconnect = true);
    }

    internal interface IRoomService
    {
        void RegisterPackets(ServerPktReg registry);
        bool TryGetHosted(PlayerConnection player, out GameRoom room);
        void HandleStateRequest(PlayerConnection player);
        void JoinPlayer(PlayerConnection player, GameRoom room);
        void Leave(PlayerConnection player, bool notify);
        void SetTrackData(GameRoom room, string trackName);
        bool SetTrackData(GameRoom room, TrackPackageRef track);
        void ShuffleNumbersForGameStart(GameRoom room);
        uint TouchVersion(GameRoom room);
        int FindFreeNumber(GameRoom room);
    }

    internal interface IRaceService
    {
        void RegisterPackets(ServerPktReg registry);
        void UpdateCompletions();
        void UpdateStopState(GameRoom room);
        int GetMinimumParticipantsToStart(GameRoom room);
        void TransitionRaceState(GameRoom room, RoomRaceState nextState, RoomRaceAbortReason abortReason = RoomRaceAbortReason.None);
        void AssignRandomBotLoadouts(GameRoom room);
        void AnnounceBotsReady(GameRoom room);
        void TryStartAfterLoadout(GameRoom room);
        void CancelPrepare(GameRoom room, PlayerConnection host);
        void SetPaused(GameRoom room, PlayerConnection host, bool paused);
        void StopWithoutResults(GameRoom room, PlayerConnection host);
        void MarkParticipantDnf(GameRoom room, uint playerId, byte playerNumber);
        bool ResolveBotFinish(GameRoom room, RoomBot bot, float finishY, out byte finishOrder);
    }

    internal interface INotifyService
    {
        void SendRoomList(PlayerConnection player);
        void SendRoomState(PlayerConnection player, GameRoom? room);
        void SendRoomGet(PlayerConnection player, GameRoom? room);
        void BroadcastRoomState(GameRoom room);
        void RoomLifecycle(GameRoom room, RoomEventKind kind);
        void RoomParticipant(GameRoom room, RoomEventKind kind, uint playerId, byte playerNumber, PlayerState state, string name);
        void ReplayRoomEventsTo(PlayerConnection player, GameRoom room, uint afterSequence);
        void ToRoom(GameRoom room, byte[] payload, PacketStream stream);
        void ToRoom(GameRoom room, byte[] payload, PacketStream stream, PacketDeliveryKind delivery);
        void ToRoomExcept(GameRoom room, uint exceptPlayerId, byte[] payload, PacketStream stream);
        void ToRoomExcept(GameRoom room, uint exceptPlayerId, byte[] payload, PacketStream stream, PacketDeliveryKind delivery);
        void ToPlayer(PlayerConnection player, byte[] payload, PacketStream stream);
        void ToPlayer(PlayerConnection player, byte[] payload, PacketStream stream, PacketDeliveryKind delivery);
        void ProtocolToRoom(GameRoom room, string text);
        void ProtocolToRoomExcept(GameRoom room, uint exceptPlayerId, string text);
        void ProtocolToLobby(string text);
        void RaceStateChanged(GameRoom room);
        void RacePlayerFinished(GameRoom room, uint playerId, byte playerNumber, byte finishOrder, int timeMs);
        void RaceCompleted(GameRoom room);
        void SendRaceCompletionTo(PlayerConnection player, GameRoom room);
        void RaceAborted(GameRoom room, RoomRaceAbortReason reason);
    }

    internal interface IRuntimeService
    {
        void Update(float deltaSeconds);
    }

    internal interface IChatService
    {
        void RegisterPackets(ServerPktReg registry);
    }

    internal interface IMediaService
    {
        void RegisterPackets(ServerPktReg registry);
    }

    internal interface ILiveService
    {
        void RegisterPackets(ServerPktReg registry);
    }
}

