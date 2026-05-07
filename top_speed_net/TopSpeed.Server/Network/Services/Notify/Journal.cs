using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal static class RoomEventJournal
    {
        public static void ClearForRaceStart(GameRoom room)
        {
            room?.EventJournal.Clear();
        }

        public static bool Record(GameRoom room, Command command, uint sequence, byte[] payload, PacketStream stream)
        {
            if (room == null || payload == null || sequence == 0)
                return false;

            if (room.EventJournal.Count > 0 && room.EventJournal[room.EventJournal.Count - 1].Sequence >= sequence)
                return false;

            room.EventJournal.Add(new RoomEventJournalEntry
            {
                Sequence = sequence,
                RaceInstanceId = room.RaceInstanceId,
                Command = command,
                Stream = stream,
                Payload = payload
            });
            return true;
        }

        public static IEnumerable<RoomEventJournalEntry> ReplayAfter(GameRoom room, uint afterSequence)
        {
            if (room == null)
                yield break;

            for (var i = 0; i < room.EventJournal.Count; i++)
            {
                var entry = room.EventJournal[i];
                if (afterSequence != 0 && entry.Sequence <= afterSequence)
                    continue;

                yield return entry;
            }
        }
    }
}

