using System;

namespace TopSpeed.Protocol
{
    public static class ConnectionRecoveryRules
    {
        public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(20);
        public static readonly TimeSpan DefaultHeartbeatMissWindow = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan DefaultReconnectGrace = TimeSpan.FromSeconds(20);

        public static bool IsRecoverableState(ConnectionLifecycleState state)
        {
            return state == ConnectionLifecycleState.SuspectedLost || state == ConnectionLifecycleState.Reconnecting;
        }

        public static bool IsGraceExpired(ConnectionLifecycleState state, DateTime? suspendedUtc, DateTime nowUtc, TimeSpan grace)
        {
            if (!IsRecoverableState(state) || !suspendedUtc.HasValue)
                return false;

            return nowUtc - suspendedUtc.Value > grace;
        }

        public static bool CanResume(
            ConnectionLifecycleState state,
            DateTime? suspendedUtc,
            DateTime nowUtc,
            TimeSpan grace,
            uint playerId,
            ulong resumeToken,
            uint requestedPlayerId,
            ulong requestedResumeToken)
        {
            return CanResume(
                state,
                suspendedUtc,
                nowUtc,
                grace,
                playerId,
                resumeToken,
                requestedPlayerId,
                requestedResumeToken,
                remoteIpMatches: true);
        }

        public static bool CanResume(
            ConnectionLifecycleState state,
            DateTime? suspendedUtc,
            DateTime nowUtc,
            TimeSpan grace,
            uint playerId,
            ulong resumeToken,
            uint requestedPlayerId,
            ulong requestedResumeToken,
            bool remoteIpMatches)
        {
            if (!IsRecoverableState(state))
                return false;
            if (IsGraceExpired(state, suspendedUtc, nowUtc, grace))
                return false;
            if (!remoteIpMatches)
                return false;
            if (playerId == 0 || requestedPlayerId != playerId)
                return false;
            if (resumeToken == 0 || requestedResumeToken != resumeToken)
                return false;

            return true;
        }
    }
}
