using TopSpeed.Localization;
using TopSpeed.Network;
using TopSpeed.Core.Multiplayer;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private bool TryBeginSessionReconnect()
        {
            var session = _session;
            if (session == null)
                return false;

            var wasInRace = _state == AppState.MultiplayerRace && _multiplayerRaceRuntime.Mode != null;
            if (!_sessionReconnector.Begin(session, wasInRace))
                return false;

            _multiplayerCoordinator.SetClientState(MultiplayerClientState.Reconnecting);
            session.SetPacketSink(null);
            session.Dispose();
            _session = null;
            ClearQueuedMultiplayerPackets();
            _speech.Speak(LocalizationService.Mark("Connection lost. Reconnecting."));
            return true;
        }

        private void UpdateSessionReconnect()
        {
            if (!_sessionReconnector.TryComplete(out var result, out var wasInRace))
                return;

            if (result.Success && result.Session != null)
            {
                AttachReconnectedSession(result.Session, wasInRace);
                _speech.Speak(LocalizationService.Mark("Reconnected to server."));
                return;
            }

            _speech.Speak(string.IsNullOrWhiteSpace(result.Message)
                ? LocalizationService.Mark("Reconnection failed.")
                : result.Message);
            DisconnectFromServer();
        }

        private void CancelSessionReconnect()
        {
            _sessionReconnector.Cancel();
        }

        private void AttachReconnectedSession(MultiplayerSession session, bool wasInRace)
        {
            _session = session;
            _multiplayerCoordinator.SetClientState(wasInRace ? MultiplayerClientState.Racing : MultiplayerClientState.Lobby);
            ClearQueuedMultiplayerPackets();
            session.SetPacketSink(packet => _multiplayerDispatch.Enqueue(session, packet));
            if (wasInRace)
                _multiplayerRaceRuntime.ReplaceNetworkSession(session);
        }

        private void HandleMultiplayerDisconnect(string message, bool explicitDisconnect)
        {
            if (!explicitDisconnect && TryBeginSessionReconnect())
                return;

            _speech.Speak(message);
            DisconnectFromServer();
        }
    }
}
