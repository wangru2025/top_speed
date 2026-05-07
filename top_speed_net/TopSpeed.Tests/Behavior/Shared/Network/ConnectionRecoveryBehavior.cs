using System;
using FluentAssertions;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Tests.Behavior.Shared.Network
{
    public sealed class ConnectionRecoveryBehavior
    {
        [Fact]
        public void Suspended_connection_can_resume_with_matching_id_token_and_grace()
        {
            var suspendedAt = DateTime.UtcNow;

            ConnectionRecoveryRules.CanResume(
                ConnectionLifecycleState.Suspended,
                suspendedAt,
                suspendedAt + TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(20),
                playerId: 12,
                resumeToken: 99,
                requestedPlayerId: 12,
                requestedResumeToken: 99).Should().BeTrue();
        }

        [Theory]
        [InlineData(ConnectionLifecycleState.Connected, 12u, 99ul)]
        [InlineData(ConnectionLifecycleState.Closed, 12u, 99ul)]
        [InlineData(ConnectionLifecycleState.Expired, 12u, 99ul)]
        [InlineData(ConnectionLifecycleState.Suspended, 13u, 99ul)]
        [InlineData(ConnectionLifecycleState.Suspended, 12u, 100ul)]
        [InlineData(ConnectionLifecycleState.Suspended, 12u, 0ul)]
        public void Resume_is_rejected_for_non_recoverable_state_or_identity_mismatch(
            ConnectionLifecycleState state,
            uint requestedPlayerId,
            ulong requestedResumeToken)
        {
            var suspendedAt = DateTime.UtcNow;

            ConnectionRecoveryRules.CanResume(
                state,
                suspendedAt,
                suspendedAt + TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(20),
                playerId: 12,
                resumeToken: 99,
                requestedPlayerId: requestedPlayerId,
                requestedResumeToken: requestedResumeToken).Should().BeFalse();
        }

        [Fact]
        public void Resume_is_rejected_after_grace_expires()
        {
            var suspendedAt = DateTime.UtcNow;

            ConnectionRecoveryRules.CanResume(
                ConnectionLifecycleState.Suspended,
                suspendedAt,
                suspendedAt + TimeSpan.FromSeconds(21),
                TimeSpan.FromSeconds(20),
                playerId: 12,
                resumeToken: 99,
                requestedPlayerId: 12,
                requestedResumeToken: 99).Should().BeFalse();
        }

        [Fact]
        public void Resume_is_rejected_when_remote_ip_does_not_match()
        {
            var suspendedAt = DateTime.UtcNow;

            ConnectionRecoveryRules.CanResume(
                ConnectionLifecycleState.Suspended,
                suspendedAt,
                suspendedAt + TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(20),
                playerId: 12,
                resumeToken: 99,
                requestedPlayerId: 12,
                requestedResumeToken: 99,
                remoteIpMatches: false).Should().BeFalse();
        }
    }
}
