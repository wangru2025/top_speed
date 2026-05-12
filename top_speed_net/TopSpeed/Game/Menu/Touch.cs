using System;
using TS.Sdl.Input;
using TopSpeed.Input;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private const long MultiplayerTopZonePttDoubleTapWindowMs = 360;
        private const long MultiplayerTopZonePttPingSuppressWindowMs = 900;
        private bool _multiplayerMenuTouchZonesApplied;
        private bool _multiplayerTopZoneWasActive;
        private bool _multiplayerTopZoneAwaitSecondTapForPtt;
        private long _multiplayerTopZoneSecondTapDeadlineMs;
        private bool _multiplayerTopZonePttHoldActive;
        private long _multiplayerTopZoneSuppressPingUntilMs;

        private void UpdateMultiplayerMenuTouchControls()
        {
            if (!ShouldApplyMultiplayerMenuTouchLayout())
            {
                ResetMultiplayerTopZoneGestureState();
                if (_multiplayerMenuTouchZonesApplied)
                {
                    // Avoid clearing race touch zones when we have already switched to race state.
                    if (!_driveTouchZonesApplied)
                        _input.ClearTouchZones();
                    _multiplayerMenuTouchZonesApplied = false;
                }

                return;
            }

            EnsureMultiplayerMenuTouchZones();
            HandleMultiplayerTopZoneGestures();
        }

        private bool ShouldApplyMultiplayerMenuTouchLayout()
        {
            if (!_isAndroidPlatform || _state != AppState.Menu)
                return false;

            var session = _session;
            if (session == null || !session.IsConnected)
                return false;

            return MenuTouchProfile.UsesMultiplayerZones(_menu.CurrentId);
        }

        private void EnsureMultiplayerMenuTouchZones()
        {
            if (_multiplayerMenuTouchZonesApplied)
                return;

            _input.SetTouchZones(new[]
            {
                new TouchZone(
                    MenuTouchProfile.MultiplayerTopZoneId,
                    new TouchZoneRect(0f, 0f, 1f, MenuTouchProfile.MultiplayerSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock),
                new TouchZone(
                    MenuTouchProfile.MultiplayerBottomZoneId,
                    new TouchZoneRect(0f, MenuTouchProfile.MultiplayerSplitY, 1f, 1f - MenuTouchProfile.MultiplayerSplitY),
                    priority: 20,
                    behavior: TouchZoneBehavior.Lock)
            });
            _multiplayerMenuTouchZonesApplied = true;
        }

        private void HandleMultiplayerTopZoneGestures()
        {
            var allowTopZoneGestures = !HasBlockingMultiplayerOverlay();
            UpdateTopZonePttGestureState(allowTopZoneGestures);
            if (!allowTopZoneGestures)
                return;

            HandleTopZoneCategoryGestures();
            HandleTopZoneHistoryItemGestures();
            HandleTopZonePingGesture();
            HandleTopZoneChatInputGestures();
            HandleTopZoneCommunicatorGestures();
        }

        private bool HasBlockingMultiplayerOverlay()
        {
            return _textInputPromptActive
                || _dialogs.HasActiveOverlayDialog
                || _choices.HasActiveChoiceDialog
                || _multiplayerMenuTouch.HasActiveOverlayQuestion;
        }

        private void HandleTopZoneCategoryGestures()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.SwipeUp, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.NextChatCategory();
            else if (_input.WasZoneGesturePressed(GestureIntent.SwipeDown, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.PreviousChatCategory();
        }

        private void HandleTopZoneHistoryItemGestures()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.SwipeRight, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.NextChatItem();
            else if (_input.WasZoneGesturePressed(GestureIntent.SwipeLeft, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.PreviousChatItem();
        }

        private void HandleTopZonePingGesture()
        {
            if (!_input.WasZoneGesturePressed(GestureIntent.TwoFingerTripleTap, MenuTouchProfile.MultiplayerTopZoneId))
                return;

            if (Environment.TickCount64 <= _multiplayerTopZoneSuppressPingUntilMs)
                return;

            _multiplayerMenuTouch.CheckPing();
        }

        private void HandleTopZoneChatInputGestures()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeRight, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.OpenGlobalChatHotkey();
            else if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeLeft, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.OpenRoomChatHotkey();
        }

        private void HandleTopZoneCommunicatorGestures()
        {
            if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeDown, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.ToggleCommunicator();
            else if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeUp, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.BeginCommunicatorFrequencyInput();

            if (_input.WasZoneGesturePressed(GestureIntent.TwoFingerDoubleTap, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.ToggleCommunicatorVoiceActivation();

            if (_input.WasZoneGesturePressed(GestureIntent.ThreeFingerTap, MenuTouchProfile.MultiplayerTopZoneId))
                _multiplayerMenuTouch.AnnounceCommunicatorFrequency();
        }

        private void UpdateTopZonePttGestureState(bool allowTopZoneGestures)
        {
            var hasTopTouch = _input.TryGetTouchZoneState(MenuTouchProfile.MultiplayerTopZoneId, out var topTouch) && topTouch.IsActive;
            var topFingerCount = hasTopTouch ? topTouch.FingerCount : 0;
            var topTouchStarted = hasTopTouch && !_multiplayerTopZoneWasActive;
            _multiplayerTopZoneWasActive = hasTopTouch;

            if (!hasTopTouch || topFingerCount != 1)
                _multiplayerTopZonePttHoldActive = false;

            var nowMs = Environment.TickCount64;
            if (_multiplayerTopZoneAwaitSecondTapForPtt && nowMs > _multiplayerTopZoneSecondTapDeadlineMs)
                _multiplayerTopZoneAwaitSecondTapForPtt = false;

            if (!allowTopZoneGestures)
            {
                _multiplayerTopZoneAwaitSecondTapForPtt = false;
                _multiplayerTopZonePttHoldActive = false;
                return;
            }

            if (_input.WasZoneGesturePressed(GestureIntent.Tap, MenuTouchProfile.MultiplayerTopZoneId))
            {
                _multiplayerTopZoneAwaitSecondTapForPtt = true;
                _multiplayerTopZoneSecondTapDeadlineMs = nowMs + MultiplayerTopZonePttDoubleTapWindowMs;
            }

            if (_multiplayerTopZoneAwaitSecondTapForPtt
                && topTouchStarted
                && hasTopTouch
                && topFingerCount == 1
                && nowMs <= _multiplayerTopZoneSecondTapDeadlineMs)
            {
                _multiplayerTopZonePttHoldActive = true;
                _multiplayerTopZoneAwaitSecondTapForPtt = false;
                _multiplayerTopZoneSuppressPingUntilMs = nowMs + MultiplayerTopZonePttPingSuppressWindowMs;
            }
        }

        private bool IsTopZonePttGestureHeld()
        {
            if (!_multiplayerTopZonePttHoldActive)
                return false;
            if (_state != AppState.Menu || !_multiplayerMenuTouchZonesApplied)
                return false;

            var session = _session;
            return session != null && session.IsConnected;
        }

        private void ResetMultiplayerTopZoneGestureState()
        {
            _multiplayerTopZoneWasActive = false;
            _multiplayerTopZoneAwaitSecondTapForPtt = false;
            _multiplayerTopZoneSecondTapDeadlineMs = 0;
            _multiplayerTopZonePttHoldActive = false;
            _multiplayerTopZoneSuppressPingUntilMs = 0;
        }
    }
}
