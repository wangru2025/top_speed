using System.Collections.Generic;
using TopSpeed.Core.Multiplayer.Chat;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private sealed partial class RoomUi
        {
            public void HandleRoomEvent(
                RoomEventInfo eventInfo,
                bool isCreator,
                uint localPlayerId,
                bool updatedCurrentRoom,
                bool localHostChanged)
            {
                var effects = new List<PacketEffect>();
                var suppressRemoteRoomCreatedNotice = ShouldSuppressRemoteRoomCreatedNotice(eventInfo, isCreator);

                if (eventInfo.Kind == RoomEventKind.RoomCreated && !isCreator && !suppressRemoteRoomCreatedNotice)
                    effects.Add(PacketEffect.PlaySound("room_created.ogg"));

                if (!suppressRemoteRoomCreatedNotice)
                {
                    var roomEventText = HistoryText.FromRoomEvent(eventInfo);
                    if (!string.IsNullOrWhiteSpace(roomEventText))
                        effects.Add(PacketEffect.AddRoomEventHistory(roomEventText));
                }

                if (updatedCurrentRoom)
                    AddCurrentRoomEventEffects(eventInfo, localPlayerId, effects);
                if (localHostChanged)
                    AddRoomMenuRebuildEffects(effects);
                if (updatedCurrentRoom)
                    effects.Add(PacketEffect.RebuildRoomPlayers());

                _owner.DispatchPacketEffects(effects);
            }

            public void HandleRoomRaceStateChanged(RoomRaceChange change)
            {
                if (!change.Applied)
                    return;

                var effects = new List<PacketEffect>();

                if (change.BeginLoadout)
                    effects.Add(PacketEffect.BeginRaceLoadout());
                if (change.LeaveLoadout
                    && (_owner._menu.CurrentId == MultiplayerMenuKeys.LoadoutVehicle || _owner._menu.CurrentId == MultiplayerMenuKeys.LoadoutTransmission))
                {
                    effects.Add(PacketEffect.ShowRoot(MultiplayerMenuKeys.RoomControls));
                }

                if (effects.Count > 0)
                    _owner.DispatchPacketEffects(effects);
            }

            private void AddCurrentRoomEventEffects(
                RoomEventInfo roomEvent,
                uint localPlayerId,
                List<PacketEffect> effects)
            {
                switch (roomEvent.Kind)
                {
                    case RoomEventKind.PrepareCancelled:
                    case RoomEventKind.RaceStopped:
                        var roomText = HistoryText.FromRoomEvent(roomEvent);
                        if (!string.IsNullOrWhiteSpace(roomText))
                            effects.Add(PacketEffect.Speak(roomText));
                        break;

                    case RoomEventKind.ParticipantJoined:
                        if (roomEvent.SubjectPlayerId != 0 && roomEvent.SubjectPlayerId != localPlayerId)
                        {
                            effects.Add(PacketEffect.PlaySound("room_join.ogg"));
                            effects.Add(PacketEffect.Speak(HistoryText.ParticipantJoined(roomEvent)));
                        }
                        break;

                    case RoomEventKind.ParticipantLeft:
                        if (roomEvent.SubjectPlayerId != 0 && roomEvent.SubjectPlayerId != localPlayerId)
                        {
                            effects.Add(PacketEffect.PlaySound("room_leave.ogg"));
                            effects.Add(PacketEffect.Speak(HistoryText.ParticipantLeft(roomEvent)));
                        }
                        break;

                    case RoomEventKind.BotAdded:
                        effects.Add(PacketEffect.PlaySound("room_join.ogg"));
                        effects.Add(PacketEffect.Speak(HistoryText.BotAdded(roomEvent)));
                        break;

                    case RoomEventKind.BotRemoved:
                        effects.Add(PacketEffect.PlaySound("room_leave.ogg"));
                        effects.Add(PacketEffect.Speak(HistoryText.BotRemoved(roomEvent)));
                        break;
                }
            }

            private bool ShouldSuppressRemoteRoomCreatedNotice(RoomEventInfo eventInfo, bool isCreator)
            {
                return eventInfo.Kind == RoomEventKind.RoomCreated
                    && _owner._state.Rooms.CurrentRoom.InRoom
                    && !isCreator
                    && _owner._state.Rooms.CurrentRoom.RoomId != eventInfo.RoomId;
            }
        }
    }
}
