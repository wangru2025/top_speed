using TopSpeed.Localization;
using TopSpeed.Shortcuts;
using Key = TopSpeed.Input.InputKey;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private const string MultiplayerPingShortcutActionId = "multiplayer_ping";
        private const string MultiplayerChatShortcutActionId = "multiplayer_chat";
        private const string MultiplayerRoomChatShortcutActionId = "multiplayer_room_chat";
        private const string MultiplayerRoomRulesShortcutActionId = "multiplayer_room_rules";
        private const string MultiplayerBufferPreviousItemShortcutActionId = "multiplayer_buffer_prev_item";
        private const string MultiplayerBufferNextItemShortcutActionId = "multiplayer_buffer_next_item";
        private const string MultiplayerBufferFirstItemShortcutActionId = "multiplayer_buffer_first_item";
        private const string MultiplayerBufferLastItemShortcutActionId = "multiplayer_buffer_last_item";
        private const string MultiplayerBufferPreviousCategoryShortcutActionId = "multiplayer_buffer_prev_category";
        private const string MultiplayerBufferNextCategoryShortcutActionId = "multiplayer_buffer_next_category";
        private const string MultiplayerBufferCopyFocusedItemShortcutActionId = "multiplayer_buffer_copy_focused_item";
        private const string MultiplayerShortcutScopeId = "multiplayer";

        private static readonly string[] MultiplayerScopeMenus =
        {
            "multiplayer",
            MultiplayerMenuKeys.DiscoveredServers,
            MultiplayerMenuKeys.SavedServers,
            MultiplayerMenuKeys.SavedServerForm,
            MultiplayerMenuKeys.Lobby,
            MultiplayerMenuKeys.RoomBrowser,
            MultiplayerMenuKeys.CreateRoom,
            MultiplayerMenuKeys.RoomControls,
            MultiplayerMenuKeys.RoomPlayers,
            MultiplayerMenuKeys.OnlinePlayers,
            MultiplayerMenuKeys.RoomOptions,
            MultiplayerMenuKeys.RoomGameRules,
            MultiplayerMenuKeys.RoomTrackType,
            MultiplayerMenuKeys.RoomTrackRace,
            MultiplayerMenuKeys.RoomTrackAdventure,
            MultiplayerMenuKeys.RoomTrackCustom,
            MultiplayerMenuKeys.RoomTrackLocalCustom,
            MultiplayerMenuKeys.LoadoutVehicle,
            MultiplayerMenuKeys.LoadoutTransmission
        };

        internal void ConfigureMenuCloseHandlersCore()
        {
            _menu.RegisterShortcutAction(
                MultiplayerPingShortcutActionId,
                LocalizationService.Mark("Check ping"),
                LocalizationService.Mark("Speaks your current ping while you are in multiplayer menus."),
                Key.F1,
                ShortcutModifiers.None,
                CheckCurrentPing);

            _menu.RegisterShortcutAction(
                MultiplayerChatShortcutActionId,
                LocalizationService.Mark("Open global chat"),
                LocalizationService.Mark("Opens chat input for the global multiplayer lobby chat."),
                Key.Slash,
                ShortcutModifiers.None,
                OpenGlobalChatInput);

            _menu.RegisterShortcutAction(
                MultiplayerRoomChatShortcutActionId,
                LocalizationService.Mark("Open room chat"),
                LocalizationService.Mark("Opens chat input for the current room chat when you are inside a room."),
                Key.Backslash,
                ShortcutModifiers.None,
                OpenRoomChatInput,
                () => IsInRoomCore);

            _menu.RegisterShortcutAction(
                MultiplayerRoomRulesShortcutActionId,
                LocalizationService.Mark("View game rules"),
                LocalizationService.Mark("Speaks currently active game rules for the current game room."),
                Key.R,
                ShortcutModifiers.None,
                AnnounceCurrentRoomGameRules,
                () => IsInRoomCore);

            _menu.RegisterShortcutAction(
                MultiplayerBufferPreviousItemShortcutActionId,
                LocalizationService.Mark("Previous history item"),
                LocalizationService.Mark("Moves to the previous item in the selected multiplayer history category."),
                Key.Comma,
                ShortcutModifiers.None,
                PreviousChatItem);

            _menu.RegisterShortcutAction(
                MultiplayerBufferNextItemShortcutActionId,
                LocalizationService.Mark("Next history item"),
                LocalizationService.Mark("Moves to the next item in the selected multiplayer history category."),
                Key.Period,
                ShortcutModifiers.None,
                NextChatItem);

            _menu.RegisterShortcutAction(
                MultiplayerBufferFirstItemShortcutActionId,
                LocalizationService.Mark("First history item"),
                LocalizationService.Mark("Moves to the first item in the selected multiplayer history category."),
                Key.Comma,
                new ShortcutModifiers(shift: true, control: false, alt: false),
                FirstChatItem);

            _menu.RegisterShortcutAction(
                MultiplayerBufferLastItemShortcutActionId,
                LocalizationService.Mark("Last history item"),
                LocalizationService.Mark("Moves to the last item in the selected multiplayer history category."),
                Key.Period,
                new ShortcutModifiers(shift: true, control: false, alt: false),
                LastChatItem);

            _menu.RegisterShortcutAction(
                MultiplayerBufferPreviousCategoryShortcutActionId,
                LocalizationService.Mark("Previous history category"),
                LocalizationService.Mark("Moves to the previous multiplayer history category."),
                Key.LeftBracket,
                ShortcutModifiers.None,
                PreviousChatCategory);

            _menu.RegisterShortcutAction(
                MultiplayerBufferNextCategoryShortcutActionId,
                LocalizationService.Mark("Next history category"),
                LocalizationService.Mark("Moves to the next multiplayer history category."),
                Key.RightBracket,
                ShortcutModifiers.None,
                NextChatCategory);

            _menu.RegisterShortcutAction(
                MultiplayerBufferCopyFocusedItemShortcutActionId,
                LocalizationService.Mark("Copy focused history item"),
                LocalizationService.Mark("Copies the currently focused multiplayer history item to the clipboard."),
                Key.Space,
                new ShortcutModifiers(shift: false, control: true, alt: false),
                CopyFocusedChatItem);

            _menu.SetScopeShortcutActions(
                MultiplayerShortcutScopeId,
                new[]
                {
                    MultiplayerPingShortcutActionId,
                    MultiplayerChatShortcutActionId,
                    MultiplayerRoomChatShortcutActionId,
                    MultiplayerBufferPreviousItemShortcutActionId,
                    MultiplayerBufferNextItemShortcutActionId,
                    MultiplayerBufferFirstItemShortcutActionId,
                    MultiplayerBufferLastItemShortcutActionId,
                    MultiplayerBufferPreviousCategoryShortcutActionId,
                    MultiplayerBufferNextCategoryShortcutActionId,
                    MultiplayerBufferCopyFocusedItemShortcutActionId
                },
                LocalizationService.Mark("Multiplayer shortcuts"));

            for (var i = 0; i < MultiplayerScopeMenus.Length; i++)
            {
                _menu.SetMenuShortcutScopes(
                    MultiplayerScopeMenus[i],
                    new[] { MultiplayerShortcutScopeId });
            }

            _menu.SetClose(MultiplayerMenuKeys.Lobby, HandleLobbyClose);
            _menu.SetClose(MultiplayerMenuKeys.RoomControls, HandleRoomControlsClose);
            _menu.SetClose(MultiplayerMenuKeys.SavedServerForm, HandleSavedServerFormClose);
            _menu.SetClose(MultiplayerMenuKeys.RoomOptions, HandleRoomOptionsClose);
            _menu.SetClose(MultiplayerMenuKeys.RoomGameRules, HandleRoomGameRulesClose);

            _menu.SetMenuShortcutActions(
                MultiplayerMenuKeys.RoomControls,
                new[] { MultiplayerRoomRulesShortcutActionId },
                LocalizationService.Mark("Room controls"));

            _menu.SetClose(MultiplayerMenuKeys.LoadoutVehicle, HandleLoadoutVehicleClose);
        }
    }
}
