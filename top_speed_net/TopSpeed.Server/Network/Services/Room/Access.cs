using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;
using TopSpeed.Server.Tracks;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public bool TryGetHosted(PlayerConnection player, out GameRoom room)
            {
                room = null!;
                if (!player.RoomId.HasValue)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                    return false;
                }

                if (!_owner._rooms.TryGetValue(player.RoomId.Value, out var foundRoom) || foundRoom == null)
                {
                    player.RoomId = null;
                    player.Live = null;
                    player.Voice = null;
                    _owner._notify.SendRoomState(player, null);
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotInRoom, LocalizationService.Mark("You are not in a game room."));
                    return false;
                }

                room = foundRoom;
                if (room.HostId != player.Id)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.NotHost, LocalizationService.Mark("Only host can do this."));
                    return false;
                }

                return true;
            }

            public void SetTrackData(GameRoom room, string trackName)
            {
                room.TrackName = trackName;
                room.TrackSelection = TrackPackageRef.BuiltIn(trackName);
                room.TrackData = TrackLoader.LoadTrack(room.TrackName, room.Laps, _owner._logger);
                room.TrackSelected = true;
                _owner.ResetRoomPackageReadiness(room);
            }

            public bool SetTrackData(GameRoom room, TrackPackageRef track)
            {
                if (track == null || !track.IsCustomPackage)
                    return false;
                if (!_owner.TryGetPackage(track.Hash, out var package))
                    return false;

                room.TrackSelection = TrackPackageRef.Custom(package.Ref.TrackId, package.Ref.Version, package.Ref.Hash);
                room.TrackName = room.TrackSelection.TrackId;
                room.TrackData = package.TrackData.WithLaps(room.Laps);
                room.TrackSelected = true;
                _owner.ResetRoomPackageReadiness(room);
                return true;
            }
        }
    }
}

