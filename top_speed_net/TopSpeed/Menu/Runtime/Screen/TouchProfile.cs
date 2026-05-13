using System;
using System.Collections.Generic;

namespace TopSpeed.Menu
{
    internal static class MenuTouchProfile
    {
        public const string MultiplayerTopZoneId = "menu_multiplayer_top";
        public const string MultiplayerBottomZoneId = "menu_multiplayer_bottom";
        public const float MultiplayerSplitY = 0.5f;
        private static readonly HashSet<string> MultiplayerZoneMenuIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "multiplayer_lobby",
            "multiplayer_rooms",
            "multiplayer_create_room",
            "multiplayer_room_controls",
            "multiplayer_room_players",
            "multiplayer_online_players",
            "multiplayer_room_options",
            "multiplayer_room_game_rules",
            "multiplayer_room_track_type",
            "multiplayer_room_tracks_race",
            "multiplayer_room_tracks_adventure",
            "multiplayer_room_tracks_custom",
            "multiplayer_room_tracks_local_custom",
            "multiplayer_loadout_vehicle",
            "multiplayer_loadout_transmission"
        };

        public static bool UsesMultiplayerZones(string? menuId)
        {
            return !string.IsNullOrWhiteSpace(menuId) &&
                MultiplayerZoneMenuIds.Contains(menuId!);
        }
    }
}
