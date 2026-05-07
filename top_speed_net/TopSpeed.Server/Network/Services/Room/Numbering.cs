using System.Collections.Generic;
using System.Linq;
using TopSpeed.Localization;
using TopSpeed.Protocol;
using TopSpeed.Server.Bots;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Room
        {
            public void CompactNumbers(RaceRoom room)
            {
                if (room == null || room.RaceStarted || room.PreparingRace)
                    return;

                var humans = room.PlayerIds
                    .Where(id => _owner._players.TryGetValue(id, out _))
                    .Select(id => _owner._players[id])
                    .OrderBy(player => player.PlayerNumber)
                    .ThenBy(player => player.Id)
                    .ToList();

                var bots = room.Bots
                    .OrderBy(bot => bot.PlayerNumber)
                    .ThenBy(bot => bot.AddedOrder)
                    .ThenBy(bot => bot.Id)
                    .ToList();

                var changedPlayers = new List<PlayerConnection>();
                var changedBots = new List<RoomBot>();
                var next = 0;

                for (var i = 0; i < humans.Count; i++)
                {
                    var expected = (byte)next++;
                    if (humans[i].PlayerNumber == expected)
                        continue;

                    humans[i].PlayerNumber = expected;
                    changedPlayers.Add(humans[i]);
                }

                for (var i = 0; i < bots.Count; i++)
                {
                    var expected = (byte)next++;
                    if (bots[i].PlayerNumber == expected)
                        continue;

                    bots[i].PlayerNumber = expected;
                    changedBots.Add(bots[i]);
                }

                if (changedPlayers.Count == 0 && changedBots.Count == 0)
                    return;

                TouchVersion(room);

                for (var i = 0; i < changedPlayers.Count; i++)
                {
                    var changed = changedPlayers[i];
                    _owner.SendStream(changed, PacketSerializer.WritePlayerNumber(changed.Id, changed.PlayerNumber), PacketStream.Control);
                    _owner._notify.RoomParticipant(
                        room,
                        RoomEventKind.ParticipantStateChanged,
                        changed.Id,
                        changed.PlayerNumber,
                        changed.State,
                        string.IsNullOrWhiteSpace(changed.Name)
                            ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), changed.PlayerNumber + 1)
                            : changed.Name);
                }

                for (var i = 0; i < changedBots.Count; i++)
                {
                    var changed = changedBots[i];
                    _owner._notify.RoomParticipant(
                        room,
                        RoomEventKind.ParticipantStateChanged,
                        changed.Id,
                        changed.PlayerNumber,
                        changed.State,
                        FormatBotDisplayName(changed));
                }

                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                var changedCount = changedPlayers.Count + changedBots.Count;
                if (changedCount > 1 || changedBots.Count > 0)
                {
                    _owner._logger.Debug(LocalizationService.Format(
                        LocalizationService.Mark("Room numbers reassigned: room={0}, players={1}, bots={2}."),
                        room.Id,
                        changedPlayers.Count,
                        changedBots.Count));
                }
            }

            public void ShuffleNumbersForRaceStart(RaceRoom room)
            {
                if (room == null || room.RaceStarted)
                    return;

                var humans = room.PlayerIds
                    .Where(id => _owner._players.TryGetValue(id, out _))
                    .Select(id => _owner._players[id])
                    .ToList();

                var bots = room.Bots.ToList();
                var participantCount = humans.Count + bots.Count;
                if (participantCount <= 1)
                    return;

                var slots = new byte[participantCount];
                for (var i = 0; i < slots.Length; i++)
                    slots[i] = (byte)i;

                for (var i = slots.Length - 1; i > 0; i--)
                {
                    var j = _owner._random.Next(i + 1);
                    (slots[i], slots[j]) = (slots[j], slots[i]);
                }

                var changedPlayers = new List<PlayerConnection>();
                var changedBots = new List<RoomBot>();
                var next = 0;

                for (var i = 0; i < humans.Count; i++)
                {
                    var expected = slots[next++];
                    if (humans[i].PlayerNumber == expected)
                        continue;

                    humans[i].PlayerNumber = expected;
                    changedPlayers.Add(humans[i]);
                }

                for (var i = 0; i < bots.Count; i++)
                {
                    var expected = slots[next++];
                    if (bots[i].PlayerNumber == expected)
                        continue;

                    bots[i].PlayerNumber = expected;
                    changedBots.Add(bots[i]);
                }

                if (changedPlayers.Count == 0 && changedBots.Count == 0)
                    return;

                TouchVersion(room);

                for (var i = 0; i < changedPlayers.Count; i++)
                {
                    var changed = changedPlayers[i];
                    _owner.SendStream(changed, PacketSerializer.WritePlayerNumber(changed.Id, changed.PlayerNumber), PacketStream.Control);
                    _owner._notify.RoomParticipant(
                        room,
                        RoomEventKind.ParticipantStateChanged,
                        changed.Id,
                        changed.PlayerNumber,
                        changed.State,
                        string.IsNullOrWhiteSpace(changed.Name)
                            ? LocalizationService.Format(LocalizationService.Mark("Player {0}"), changed.PlayerNumber + 1)
                            : changed.Name);
                }

                for (var i = 0; i < changedBots.Count; i++)
                {
                    var changed = changedBots[i];
                    _owner._notify.RoomParticipant(
                        room,
                        RoomEventKind.ParticipantStateChanged,
                        changed.Id,
                        changed.PlayerNumber,
                        changed.State,
                        FormatBotDisplayName(changed));
                }

                _owner._notify.RoomLifecycle(room, RoomEventKind.RoomSummaryUpdated);
                _owner._logger.Debug(LocalizationService.Format(
                    LocalizationService.Mark("Room race-start numbers shuffled: room={0}, players={1}, bots={2}."),
                    room.Id,
                    changedPlayers.Count,
                    changedBots.Count));
            }
        }
    }
}
