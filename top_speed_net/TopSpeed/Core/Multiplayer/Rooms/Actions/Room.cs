using TopSpeed.Localization;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenLeaveRoomConfirmation()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak(LocalizationService.Mark("You are not currently inside a game room."));
                return;
            }

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(LocalizationService.Mark("Leave this game room?"),
                LocalizationService.Mark("Are you sure you want to leave the current room?"),
                QuestionId.No,
                HandleLeaveRoomQuestionResult,
                new QuestionButton(QuestionId.Yes, LocalizationService.Mark("Yes, leave this game room")),
                new QuestionButton(QuestionId.No, LocalizationService.Mark("No, stay in this game room"), flags: QuestionButtonFlags.Default)));
        }

        private void HandleLeaveRoomQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmLeaveRoom();
        }

        private void ConfirmLeaveRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                if (_state.Connection.ClientState == MultiplayerClientState.Reconnecting)
                {
                    _speech.Speak(LocalizationService.Mark("Reconnection is in progress. Disconnecting now."));
                    Disconnect();
                    return;
                }

                _speech.Speak(LocalizationService.Mark("Not connected to a server. Returning to main menu."));
                Disconnect();
                return;
            }

            if (!TrySend(session.SendRoomLeave(), LocalizationService.Mark("room leave request")))
                return;
            _speech.Speak(LocalizationService.Mark("Leaving game room."));
            _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
        }

        private void StartGame()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak(LocalizationService.Mark("Only the host can start the game."));
                return;
            }

            TrySend(session.SendRoomStartRace(), LocalizationService.Mark("race start request"));
        }

        private void AddBotToRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak(LocalizationService.Mark("Bots can only be managed by the host in race-with-bots rooms."));
                return;
            }

            TrySend(session.SendRoomAddBot(), LocalizationService.Mark("add bot request"));
        }

        private void RemoveLastBotFromRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak(LocalizationService.Mark("Not connected to a server."));
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak(LocalizationService.Mark("Bots can only be managed by the host in race-with-bots rooms."));
                return;
            }

            TrySend(session.SendRoomRemoveBot(), LocalizationService.Mark("remove bot request"));
        }
    }
}
