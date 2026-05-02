using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void AddBot(PlayerConnection player)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room add-bot denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot add bots while race setup or race is active."));
                    return;
                }

                if (room.RoomType != GameRoomType.BotsRace)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Bots can only be added in race-with-bots rooms."));
                    return;
                }

                if (GetRoomParticipantCount(room) >= room.PlayersToStart)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.RoomFull, RoomTexts.RoomUnavailableFull);
                    return;
                }

                var bot = _owner.CreateBot(room);
                room.Bots.Add(bot);
                CompactNumbers(room);
                TouchVersion(room);
                _owner._notify.RoomParticipant(room, RoomEventKind.BotAdded, bot.Id, bot.PlayerNumber, bot.State, FormatBotDisplayName(bot));
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._notify.ToRoom(room, PacketSerializer.WritePlayerJoined(new PacketPlayerJoined
                {
                    PlayerId = bot.Id,
                    PlayerNumber = bot.PlayerNumber,
                    Name = FormatBotJoinName(bot)
                }), PacketStream.Room);
                if (room.PreparingRace)
                    _owner._race.TryStartAfterLoadout(room);
            }

            public void RemoveBot(PlayerConnection player)
            {
                if (!TryGetHosted(player, out var room))
                    return;
                if (room.RaceStarted || room.PreparingRace)
                {
                    _owner._roomMutationDenied++;
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room remove-bot denied: room={0}, player={1}, raceStarted={2}, preparing={3}."),
                        room.Id,
                        player.Id,
                        room.RaceStarted,
                        room.PreparingRace));
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Cannot remove bots while race setup or race is active."));
                    return;
                }

                if (room.RoomType != GameRoomType.BotsRace)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, LocalizationService.Mark("Bots can only be removed in race-with-bots rooms."));
                    return;
                }

                if (room.Bots.Count == 0)
                {
                    _owner.SendProtocolMessage(player, ProtocolMessageCode.Failed, RoomTexts.NoBotsToRemove);
                    return;
                }

                var bot = room.Bots.OrderByDescending(candidate => candidate.AddedOrder).First();
                room.Bots.Remove(bot);
                CompactNumbers(room);
                _owner._notify.ToRoom(room, PacketSerializer.WritePlayer(Command.PlayerDisconnected, bot.Id, bot.PlayerNumber), PacketStream.Room);
                TouchVersion(room);
                _owner._notify.RoomParticipant(room, RoomEventKind.BotRemoved, bot.Id, bot.PlayerNumber, bot.State, FormatBotDisplayName(bot));
                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner.SendProtocolMessage(
                    player,
                    ProtocolMessageCode.Ok,
                    LocalizationService.Format(
                        LocalizationService.Mark("Removed bot {0}."),
                        bot.Name));
                if (room.RaceStarted)
                    _owner._race.UpdateStopState(room);
                if (room.PreparingRace)
                    _owner._race.TryStartAfterLoadout(room);
            }
        }
    }
}
