using Key = TopSpeed.Input.InputKey;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        private bool TryHandlePendingTitle(IInputService input)
        {
            if (!_titlePending)
                return true;

            if (input.IsAnyMenuInputHeld())
                return false;

            _titlePending = false;
            AnnounceTitle();
            return true;
        }

        private UpdateInputState CaptureInputState(IInputService input)
        {
            var controlHeld = input.IsDown(Key.LeftControl) || input.IsDown(Key.RightControl);
            var shiftHeld = input.IsDown(Key.LeftShift) || input.IsDown(Key.RightShift);
            var altHeld = input.IsDown(Key.LeftAlt) || input.IsDown(Key.RightAlt);
            var tabPressed = !controlHeld && !altHeld && input.WasPressed(Key.Tab);

            var state = new UpdateInputState(
                MenuInputBindings.IsPressed(input, MenuInputAction.PreviousItem),
                MenuInputBindings.IsPressed(input, MenuInputAction.NextItem),
                MenuInputBindings.IsPressed(input, MenuInputAction.MoveHome),
                MenuInputBindings.IsPressed(input, MenuInputAction.MoveEnd),
                MenuInputBindings.IsPressed(input, MenuInputAction.MoveLeft),
                MenuInputBindings.IsPressed(input, MenuInputAction.MoveRight),
                MenuInputBindings.IsPressed(input, MenuInputAction.PageUp),
                MenuInputBindings.IsPressed(input, MenuInputAction.PageDown),
                tabPressed && !shiftHeld,
                tabPressed && shiftHeld,
                MenuInputBindings.IsPressed(input, MenuInputAction.Activate),
                MenuInputBindings.IsPressed(input, MenuInputAction.Back));

            ApplyGestureInput(input, ref state);

            if (input.TryGetControllerState(out var controller))
            {
                var useAxes = !input.IgnoreControllerAxesForMenuNavigation;
                if (!_hasControllerCenter && MenuInputUtil.IsNearCenter(controller, useAxes))
                {
                    _controllerCenter = controller;
                    _hasControllerCenter = true;
                }

                var previous = _hasPrevController ? _prevController : _controllerCenter;
                state.MoveUp |= MenuInputUtil.WasControllerUpPressed(controller, previous, useAxes);
                state.MoveDown |= MenuInputUtil.WasControllerDownPressed(controller, previous, useAxes);
                state.Activate |= MenuInputUtil.WasControllerActivatePressed(controller, previous, useAxes);
                state.Back |= MenuInputUtil.WasControllerBackPressed(controller, previous, useAxes);
                _prevController = controller;
                _hasPrevController = true;
            }
            else
            {
                _hasPrevController = false;
            }

            return state;
        }

        private void ApplyGestureInput(IInputService input, ref UpdateInputState state)
        {
            var currentItem = _index >= 0 && _index < _items.Count
                ? _items[_index]
                : null;

            if (UseMultiplayerTouchZones())
            {
                ApplyMultiplayerZoneGestureInput(input, currentItem, ref state);
                return;
            }

            var swipeLeft = input.WasGesturePressed(GestureIntent.SwipeLeft);
            var swipeRight = input.WasGesturePressed(GestureIntent.SwipeRight);
            var swipeUp = input.WasGesturePressed(GestureIntent.SwipeUp);
            var swipeDown = input.WasGesturePressed(GestureIntent.SwipeDown);
            var twoFingerSwipeLeft = input.WasGesturePressed(GestureIntent.TwoFingerSwipeLeft);
            var twoFingerSwipeRight = input.WasGesturePressed(GestureIntent.TwoFingerSwipeRight);
            var twoFingerSwipeUp = input.WasGesturePressed(GestureIntent.TwoFingerSwipeUp);
            var twoFingerSwipeDown = input.WasGesturePressed(GestureIntent.TwoFingerSwipeDown);
            var threeFingerSwipeUp = input.WasGesturePressed(GestureIntent.ThreeFingerSwipeUp);
            var threeFingerSwipeDown = input.WasGesturePressed(GestureIntent.ThreeFingerSwipeDown);

            state.MoveUp |= swipeLeft;
            state.MoveDown |= swipeRight;

            var isSlider = currentItem is Slider;
            var supportsFineAdjust = currentItem is Slider || currentItem is RadioButton || currentItem is ToggleItem;

            if (isSlider)
            {
                state.PageUp |= twoFingerSwipeUp;
                state.PageDown |= twoFingerSwipeDown;
                state.MoveHome |= threeFingerSwipeUp;
                state.MoveEnd |= threeFingerSwipeDown;
                state.Back |= swipeDown;
            }
            else
            {
                state.Activate |= swipeUp;
                state.Back |= swipeDown;
                state.MoveHome |= twoFingerSwipeUp;
                state.MoveEnd |= twoFingerSwipeDown;
            }

            if (supportsFineAdjust || (currentItem?.HasActions ?? false))
            {
                state.MoveLeft |= twoFingerSwipeLeft;
                state.MoveRight |= twoFingerSwipeRight;
            }

        }

        private bool UseMultiplayerTouchZones()
        {
            return InteractionHints.IsTouchPlatform() && MenuTouchProfile.UsesMultiplayerZones(Id);
        }

        private static void ApplyMultiplayerZoneGestureInput(IInputService input, MenuItem? currentItem, ref UpdateInputState state)
        {
            var bottomSwipeLeft = input.WasZoneGesturePressed(GestureIntent.SwipeLeft, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomSwipeRight = input.WasZoneGesturePressed(GestureIntent.SwipeRight, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomSwipeUp = input.WasZoneGesturePressed(GestureIntent.SwipeUp, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomSwipeDown = input.WasZoneGesturePressed(GestureIntent.SwipeDown, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomTwoFingerSwipeLeft = input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeLeft, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomTwoFingerSwipeRight = input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeRight, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomTwoFingerSwipeUp = input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeUp, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomTwoFingerSwipeDown = input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeDown, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomThreeFingerSwipeUp = input.WasZoneGesturePressed(GestureIntent.ThreeFingerSwipeUp, MenuTouchProfile.MultiplayerBottomZoneId);
            var bottomThreeFingerSwipeDown = input.WasZoneGesturePressed(GestureIntent.ThreeFingerSwipeDown, MenuTouchProfile.MultiplayerBottomZoneId);

            // Left/right maps to previous/next item in menu ordering.
            state.MoveUp |= bottomSwipeLeft;
            state.MoveDown |= bottomSwipeRight;

            var isSlider = currentItem is Slider;
            var supportsFineAdjust = currentItem is Slider || currentItem is RadioButton || currentItem is ToggleItem;

            if (isSlider)
            {
                state.PageUp |= bottomTwoFingerSwipeUp;
                state.PageDown |= bottomTwoFingerSwipeDown;
                state.MoveHome |= bottomThreeFingerSwipeUp;
                state.MoveEnd |= bottomThreeFingerSwipeDown;
                state.Back |= bottomSwipeDown;
            }
            else
            {
                state.Activate |= bottomSwipeUp;
                state.Back |= bottomSwipeDown;
                state.MoveHome |= bottomTwoFingerSwipeUp;
                state.MoveEnd |= bottomTwoFingerSwipeDown;
            }

            if (supportsFineAdjust || (currentItem?.HasActions ?? false))
            {
                state.MoveLeft |= bottomTwoFingerSwipeLeft;
                state.MoveRight |= bottomTwoFingerSwipeRight;
            }
        }

        private bool TryHandleHeldInputGate(IInputService input, UpdateInputState state, out MenuUpdateResult result)
        {
            result = MenuUpdateResult.None;
            if (!_ignoreHeldInput)
                return false;

            if (input.IsMenuBackHeld())
            {
                input.LatchMenuBack();
                _ignoreHeldInput = false;
                ClearAutoFocusPending();
                result = MenuUpdateResult.Back;
                return true;
            }

            if (state.MoveUp)
            {
                _ignoreHeldInput = false;
                ClearAutoFocusPending();
                MoveToIndex(_items.Count - 1);
                return true;
            }

            if (state.MoveDown)
            {
                _ignoreHeldInput = false;
                ClearAutoFocusPending();
                MoveToIndex(0);
                return true;
            }

            if (state.MoveHome)
            {
                _ignoreHeldInput = false;
                ClearAutoFocusPending();
                MoveToIndex(0);
                return true;
            }

            if (state.MoveEnd)
            {
                _ignoreHeldInput = false;
                ClearAutoFocusPending();
                MoveToIndex(_items.Count - 1);
                return true;
            }

            if (state.Activate || state.Back)
            {
                _ignoreHeldInput = false;
                return false;
            }

            if (input.IsAnyMenuInputHeld())
                return true;

            _ignoreHeldInput = false;
            input.ResetState();
            return false;
        }

        private struct UpdateInputState
        {
            public UpdateInputState(
                bool moveUp,
                bool moveDown,
                bool moveHome,
                bool moveEnd,
                bool moveLeft,
                bool moveRight,
                bool pageUp,
                bool pageDown,
                bool nextScreen,
                bool previousScreen,
                bool activate,
                bool back)
            {
                MoveUp = moveUp;
                MoveDown = moveDown;
                MoveHome = moveHome;
                MoveEnd = moveEnd;
                MoveLeft = moveLeft;
                MoveRight = moveRight;
                PageUp = pageUp;
                PageDown = pageDown;
                NextScreen = nextScreen;
                PreviousScreen = previousScreen;
                Activate = activate;
                Back = back;
            }

            public bool MoveUp;
            public bool MoveDown;
            public bool MoveHome;
            public bool MoveEnd;
            public bool MoveLeft;
            public bool MoveRight;
            public bool PageUp;
            public bool PageDown;
            public bool NextScreen;
            public bool PreviousScreen;
            public bool Activate;
            public bool Back;
        }
    }
}




