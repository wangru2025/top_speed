using TopSpeed.Input;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void OnSessionCleared()
        {
            _roomsFlow.OnSessionCleared();
        }

        internal void OnSessionClearedCore()
        {
            _lifetime.CancelAllOperations();
            _lifetime.ResetPing();
            _lifetime.StopNetworkAudio();
            _state.Rooms.Reset();
            ResetCreateRoomDraft();
            _state.RoomDrafts.IsRoomBrowserOpenPending = false;
            _state.RoomDrafts.IsOnlinePlayersOpenPending = false;
            _state.RoomDrafts.PendingLoadoutVehicleIndex = 0;
            _state.RoomDrafts.RoomOptionsDraftActive = false;
            _state.RoomDrafts.RoomOptionsTrackName = string.Empty;
            _state.RoomDrafts.RoomOptionsTrackRandom = false;
            _state.RoomDrafts.RoomOptionsLaps = 1;
            _state.RoomDrafts.RoomOptionsPlayersToStart = 2;
            _state.RoomDrafts.RoomOptionsGameRulesFlags = 0;
            _state.SavedServers.Draft = new SavedServerEntry();
            _state.SavedServers.Original = null;
            _state.SavedServers.EditIndex = -1;
            _state.SavedServers.PendingDeleteIndex = -1;
            _state.Connection.HasPendingCompatibilityResult = false;
            _state.Connection.PendingCompatibilityResult = default;
            SetClientState(MultiplayerClientState.Disconnected);
            _state.Chat.History.Clear();
            RebuildLobbyMenu();
            RebuildCreateRoomMenu();
            RebuildSavedServersMenu();
            RebuildSavedServerFormMenu();
            RebuildRoomControlsMenu();
            RebuildRoomOptionsMenu();
            RebuildRoomGameRulesMenu();
            RebuildRoomPlayersMenu();
            RebuildOnlinePlayersMenu();
            RebuildLoadoutVehicleMenu();
            RebuildLoadoutTransmissionMenu();
            UpdateRoomBrowserMenu();
            UpdateHistoryScreens();
        }
    }
}




