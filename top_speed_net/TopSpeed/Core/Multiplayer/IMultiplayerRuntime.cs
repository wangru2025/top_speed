using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal interface IMultiplayerRuntime
    {
        QuestionDialog Questions { get; }
        bool IsInRoom { get; }
        bool IsCurrentRoomHost { get; }
        bool IsCurrentRacePaused { get; }
        MultiplayerClientState ClientState { get; }

        void ConfigureMenuCloseHandlers();
        void ShowMultiplayerMenuAfterRace();
        void BeginRaceLoadoutSelection();

        void BeginManualServerEntry();
        void BeginServerPortEntry();
        void BeginDefaultCallSignEntry();
        void StartServerDiscovery();
        void OpenSavedServersManager();
        bool UpdatePendingOperations();
        void OnSessionCleared();
        void SetClientState(MultiplayerClientState state);

        void NextChatCategory();
        void PreviousChatCategory();
        void NextChatItem();
        void PreviousChatItem();
        void CheckPing();
        void OpenGlobalChatHotkey();
        void OpenRoomChatHotkey();

        void HandlePingReply(long receivedUtcTicks = 0);
        void HandleRoomList(PacketRoomList roomList);
        void HandleRoomState(PacketRoomState roomState);
        void HandleRoomEvent(PacketRoomEvent roomEvent);
        void HandleRoomRaceStateChanged(PacketRoomRaceStateChanged roomRaceStateChanged);
        void HandleOnlinePlayers(PacketOnlinePlayers onlinePlayers);
        void HandleProtocolMessage(PacketProtocolMessage message);

        string ResolvePlayerName(byte playerNumber);
    }
}

