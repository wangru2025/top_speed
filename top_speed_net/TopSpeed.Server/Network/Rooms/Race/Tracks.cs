using System;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private void SendTrackToRoom(RaceRoom room)
        {
            foreach (var id in room.PlayerIds)
            {
                if (_players.TryGetValue(id, out var player))
                    SendTrack(room, player);
            }
        }

        private void SendTrack(RaceRoom room, PlayerConnection player)
        {
            if (!room.TrackSelected || room.TrackData == null)
                return;

            if (room.TrackSelection != null && room.TrackSelection.IsCustomPackage)
            {
                if (TryGetTrackPackage(room.TrackSelection.Hash, out var package))
                    SendTrackPackageToPlayer(player, package);
                return;
            }

            var trackLength = (ushort)Math.Min(room.TrackData.Definitions.Length, ProtocolConstants.MaxMultiTrackLength);
            SendStream(player, PacketSerializer.WriteLoadCustomTrack(new PacketLoadCustomTrack
            {
                NrOfLaps = room.TrackData.Laps,
                TrackName = room.TrackData.UserDefined ? "custom" : room.TrackName,
                TrackAmbience = room.TrackData.Ambience,
                DefaultWeatherProfileId = room.TrackData.DefaultWeatherProfileId,
                WeatherProfiles = room.TrackData.WeatherProfiles,
                TrackLength = trackLength,
                Definitions = room.TrackData.Definitions
            }), PacketStream.Room);
        }

    }
}
