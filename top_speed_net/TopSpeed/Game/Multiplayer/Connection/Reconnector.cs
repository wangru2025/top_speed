using System;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Localization;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Game
{
    internal sealed class SessionReconnector : IDisposable
    {
        private const int MaxAttempts = 3;

        private readonly MultiplayerConnector _connector;
        private Task<ConnectResult>? _task;
        private CancellationTokenSource? _cts;
        private bool _wasInRace;

        public SessionReconnector(MultiplayerConnector connector)
        {
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            State = ConnectionLifecycleState.Closed;
        }

        public ConnectionLifecycleState State { get; private set; }
        public bool IsActive => _task != null;

        public bool Begin(MultiplayerSession session, bool wasInRace)
        {
            if (_task != null)
                return true;
            if (session == null || session.PlayerId == 0 || session.ResumeToken == 0)
                return false;

            _wasInRace = wasInRace;
            _cts = new CancellationTokenSource();
            State = ConnectionLifecycleState.Reconnecting;
            _task = ReconnectAsync(
                session.Address.ToString(),
                session.Port,
                session.PlayerName,
                session.PlayerId,
                session.ResumeToken,
                _cts.Token);
            return true;
        }

        public bool TryComplete(out ConnectResult result, out bool wasInRace)
        {
            result = default;
            wasInRace = false;
            var task = _task;
            if (task == null || !task.IsCompleted)
                return false;

            result = task.IsFaulted || task.IsCanceled
                ? ConnectResult.CreateFail(LocalizationService.Mark("Reconnection failed."))
                : task.GetAwaiter().GetResult();
            wasInRace = _wasInRace;
            State = result.Success ? ConnectionLifecycleState.Resumed : ConnectionLifecycleState.Closed;
            Clear();
            return true;
        }

        public void Cancel()
        {
            if (_task == null && _cts == null)
                return;

            try
            {
                _cts?.Cancel();
            }
            catch
            {
            }

            State = ConnectionLifecycleState.Closed;
            Clear();
        }

        public void Dispose()
        {
            Cancel();
        }

        private void Clear()
        {
            _task = null;
            _cts?.Dispose();
            _cts = null;
            _wasInRace = false;
        }

        private async Task<ConnectResult> ReconnectAsync(string host, int port, string callSign, uint playerId, ulong resumeToken, CancellationToken token)
        {
            ConnectResult last = ConnectResult.CreateFail(LocalizationService.Mark("Reconnection failed."));
            for (var attempt = 1; attempt <= MaxAttempts && !token.IsCancellationRequested; attempt++)
            {
                last = await _connector.ConnectAsync(
                    host,
                    port,
                    callSign,
                    TimeSpan.FromSeconds(5),
                    token,
                    playerId,
                    resumeToken).ConfigureAwait(false);

                if (last.Success)
                    return last;

                if (attempt < MaxAttempts)
                    await Task.Delay(500, token).ConfigureAwait(false);
            }

            return last;
        }
    }
}
