using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private sealed partial class Notify
        {
            public void ToRoom(RaceRoom room, byte[] payload, PacketStream stream)
            {
                if (room == null || payload == null)
                    return;

                foreach (var id in room.PlayerIds)
                {
                    if (TryGetReadyPlayer(id, out var player))
                        _owner.SendStream(player, payload, stream);
                }
            }

            public void ToPlayer(PlayerConnection player, byte[] payload, PacketStream stream)
            {
                if (payload == null || !IsReady(player))
                    return;

                _owner.SendStream(player, payload, stream);
            }

            public void ToPlayer(PlayerConnection player, byte[] payload, PacketStream stream, PacketDeliveryKind delivery)
            {
                if (payload == null || !IsReady(player))
                    return;

                _owner.SendStream(player, payload, stream, delivery);
            }

            public void ToRoom(RaceRoom room, byte[] payload, PacketStream stream, PacketDeliveryKind delivery)
            {
                if (room == null || payload == null)
                    return;

                foreach (var id in room.PlayerIds)
                {
                    if (TryGetReadyPlayer(id, out var player))
                        _owner.SendStream(player, payload, stream, delivery);
                }
            }

            public void ToRoomExcept(RaceRoom room, uint exceptPlayerId, byte[] payload, PacketStream stream)
            {
                if (room == null || payload == null)
                    return;

                foreach (var id in room.PlayerIds)
                {
                    if (id == exceptPlayerId)
                        continue;
                    if (TryGetReadyPlayer(id, out var player))
                        _owner.SendStream(player, payload, stream);
                }
            }

            public void ToRoomExcept(RaceRoom room, uint exceptPlayerId, byte[] payload, PacketStream stream, PacketDeliveryKind delivery)
            {
                if (room == null || payload == null)
                    return;

                foreach (var id in room.PlayerIds)
                {
                    if (id == exceptPlayerId)
                        continue;
                    if (TryGetReadyPlayer(id, out var player))
                        _owner.SendStream(player, payload, stream, delivery);
                }
            }

            public void ToHost(RaceRoom room, byte[] payload, PacketStream stream)
            {
                if (room == null || payload == null || room.HostId == 0)
                    return;

                if (TryGetReadyPlayer(room.HostId, out var host))
                    _owner.SendStream(host, payload, stream);
            }

            public void ToAll(byte[] payload, PacketStream stream)
            {
                if (payload == null)
                    return;

                foreach (var player in _owner._players.Values)
                {
                    if (IsReady(player))
                        _owner.SendStream(player, payload, stream);
                }
            }

            public void ToLobby(byte[] payload, PacketStream stream)
            {
                if (payload == null)
                    return;

                foreach (var player in _owner._players.Values)
                {
                    if (player.RoomId.HasValue)
                        continue;
                    if (!IsReady(player))
                        continue;
                    _owner.SendStream(player, payload, stream);
                }
            }

            public void ToActiveRacers(RaceRoom room, byte[] payload, PacketStream stream)
            {
                if (room == null || payload == null)
                    return;

                foreach (var id in room.ActiveRaceParticipantIds)
                {
                    if (TryGetReadyPlayer(id, out var player))
                        _owner.SendStream(player, payload, stream);
                }
            }

            public void ReplayRoomEventsTo(PlayerConnection player, RaceRoom room, uint afterSequence)
            {
                if (player == null || room == null)
                    return;

                var replayedAny = false;
                foreach (var entry in RoomEventJournal.ReplayAfter(room, afterSequence))
                {
                    replayedAny = true;
                    ToPlayer(player, entry.Payload, entry.Stream);
                }

                if (afterSequence > 0 && !replayedAny && room.EventSequence > afterSequence)
                    _owner._replayGapCount++;

                // Replay is followed by full room snapshot to converge drift deterministically.
                SendRoomState(player, room);
            }

            public void ProtocolToRoom(RaceRoom room, string text)
            {
                var payload = BuildProtocolMessage(ProtocolMessageCode.Ok, text);
                if (payload == null)
                    return;
                ToRoom(room, payload, PacketStream.Chat);
            }

            public void ProtocolToRoomExcept(RaceRoom room, uint exceptPlayerId, string text)
            {
                var payload = BuildProtocolMessage(ProtocolMessageCode.Ok, text);
                if (payload == null)
                    return;
                ToRoomExcept(room, exceptPlayerId, payload, PacketStream.Chat);
            }

            public void ProtocolToLobby(string text)
            {
                var payload = BuildProtocolMessage(ProtocolMessageCode.Ok, text);
                if (payload == null)
                    return;
                ToLobby(payload, PacketStream.Direct);
            }

            public void ProtocolToHost(RaceRoom room, ProtocolMessageCode code, string text)
            {
                var payload = BuildProtocolMessage(code, text);
                if (payload == null)
                    return;
                ToHost(room, payload, PacketStream.Direct);
            }

            private static byte[]? BuildProtocolMessage(ProtocolMessageCode code, string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                return PacketSerializer.WriteProtocolMessage(new PacketProtocolMessage
                {
                    Code = code,
                    Message = text
                });
            }

            private bool TryGetReadyPlayer(uint playerId, out PlayerConnection player)
            {
                if (_owner._players.TryGetValue(playerId, out var candidate) && IsReady(candidate))
                {
                    player = candidate;
                    return true;
                }

                player = null!;
                return false;
            }

            private static bool IsReady(PlayerConnection player)
            {
                return player != null && player.Connected && player.Handshake == HandshakeState.Complete;
            }

            private static uint NextEventSequence(RaceRoom room)
            {
                if (room == null)
                    return 0;

                room.EventSequence++;
                if (room.EventSequence == 0)
                    room.EventSequence = 1;
                return room.EventSequence;
            }

            private static uint CurrentEventSequence(RaceRoom room)
            {
                return room?.EventSequence ?? 0;
            }
        }
    }
}
