using System;
using System.Linq;
using System.Net;
using TopSpeed.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed class ServerPlayerTestSnapshot
    {
        public uint PlayerId { get; set; }
        public byte PlayerNumber { get; set; }
        public ulong ResumeToken { get; set; }
        public uint? RoomId { get; set; }
        public ConnectionLifecycleState LifecycleState { get; set; }
        public PlayerState State { get; set; }
    }

    internal sealed class ServerStressSnapshot
    {
        public int PlayerCount { get; set; }
        public int RoomCount { get; set; }
        public int RacingRoomCount { get; set; }
        public int CompletedRoomCount { get; set; }
        public int AbortedRoomCount { get; set; }
        public int ActiveRaceParticipantCount { get; set; }
        public int FinishedResultCount { get; set; }
        public int DnfResultCount { get; set; }
        public int UnresolvedResultCount { get; set; }
        public int CompletionInvariantFailureCount { get; set; }
        public int TotalJournalEventCount { get; set; }
        public int MaxJournalEventCount { get; set; }
        public int AuthorityDropCount { get; set; }
        public int RaceSnapshotSends { get; set; }
        public int StateSyncFramesSent { get; set; }
    }

    internal sealed partial class RaceServer
    {
        internal void InjectPacketForTest(IPEndPoint endPoint, byte[] payload)
        {
            OnPacket(endPoint, payload);
        }

        internal void DisconnectPeerForTest(IPEndPoint endPoint)
        {
            OnPeerDisconnected(endPoint);
        }

        internal ServerPlayerTestSnapshot GetPlayerSnapshotForTest(uint playerId)
        {
            lock (_lock)
            {
                if (!_players.TryGetValue(playerId, out var player))
                    throw new InvalidOperationException("Player was not found.");

                return new ServerPlayerTestSnapshot
                {
                    PlayerId = player.Id,
                    PlayerNumber = player.PlayerNumber,
                    ResumeToken = player.ResumeToken,
                    RoomId = player.RoomId,
                    LifecycleState = player.LifecycleState,
                    State = player.State
                };
            }
        }

        internal uint GetRoomRaceInstanceForTest(uint roomId)
        {
            lock (_lock)
            {
                return _rooms.TryGetValue(roomId, out var room) ? room.RaceInstanceId : 0;
            }
        }

        internal ServerStressSnapshot GetStressSnapshotForTest()
        {
            lock (_lock)
            {
                var snapshot = new ServerStressSnapshot
                {
                    PlayerCount = _players.Count,
                    RoomCount = _rooms.Count,
                    RacingRoomCount = _rooms.Values.Count(room => room.RaceState == RoomRaceState.Racing),
                    CompletedRoomCount = _rooms.Values.Count(room => room.RaceState == RoomRaceState.Completed),
                    AbortedRoomCount = _rooms.Values.Count(room => room.RaceState == RoomRaceState.Aborted),
                    ActiveRaceParticipantCount = _rooms.Values.Sum(room => room.ActiveRaceParticipantIds.Count),
                    TotalJournalEventCount = _rooms.Values.Sum(room => room.EventJournal.Count),
                    MaxJournalEventCount = _rooms.Values.Select(room => room.EventJournal.Count).DefaultIfEmpty(0).Max(),
                    AuthorityDropCount =
                        _authorityDropsPlayerState +
                        _authorityDropsPlayerData +
                        _authorityDropsPlayerStarted +
                        _authorityDropsPlayerCrashed,
                    RaceSnapshotSends = _raceSnapshotSends,
                    StateSyncFramesSent = _stateSyncFramesSent
                };

                foreach (var room in _rooms.Values)
                {
                    if (room.RaceState == RoomRaceState.Completed
                        && !RaceCompletionInvariants.TryValidateTerminalResults(room, out _))
                    {
                        snapshot.CompletionInvariantFailureCount++;
                    }

                    foreach (var result in room.RaceParticipantResults.Values)
                    {
                        if (result.Status == RoomRaceResultStatus.Finished)
                            snapshot.FinishedResultCount++;
                        else if (result.Status == RoomRaceResultStatus.Dnf)
                            snapshot.DnfResultCount++;
                        else
                            snapshot.UnresolvedResultCount++;
                    }
                }

                return snapshot;
            }
        }
    }
}
