using System;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        private int ComputeSteering()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerSteer = 0;
            if (UseController)
            {
                var left = ApplySteeringDeadZone(ResolveSteerLeftAxis());
                var right = ApplySteeringDeadZone(ResolveSteerRightAxis());
                controllerSteer = left != 0 ? -left : right;
            }

            if (!UseKeyboard)
                return controllerSteer;

            var keyboardSteer = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (IsKeyDown(_lastState, _kbLeft) ? -100 : (IsKeyDown(_lastState, _kbRight) ? 100 : 0))
                : (int)(_simSteer * 100f);

            var baseSteering = Math.Abs(keyboardSteer) > Math.Abs(controllerSteer) ? keyboardSteer : controllerSteer;
            return Math.Abs(_touchSteering) > Math.Abs(baseSteering) ? _touchSteering : baseSteering;
        }

        private int ResolveSteerLeftAxis()
        {
            return UseController ? GetAxis(_left) : 0;
        }

        private int ResolveSteerRightAxis()
        {
            return UseController ? GetAxis(_right) : 0;
        }

        private int ComputeThrottle()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerThrottle = UseController ? GetPedalAxis(_throttle, _settings.ControllerThrottleInvertMode) : 0;
            if (!UseKeyboard)
                return controllerThrottle;

            var keyboardThrottle = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (IsKeyDown(_lastState, _kbThrottle) ? 100 : 0)
                : (int)(_simThrottle * 100f);

            return Math.Max(_touchThrottle, Math.Max(controllerThrottle, keyboardThrottle));
        }

        private int ComputeBrake()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerBrake = UseController ? -GetPedalAxis(_brake, _settings.ControllerBrakeInvertMode) : 0;
            if (!UseKeyboard)
                return controllerBrake;

            var keyboardBrake = _settings.KeyboardProgressiveRate == KeyboardProgressiveRate.Off
                ? (IsKeyDown(_lastState, _kbBrake) ? -100 : 0)
                : (int)(_simBrake * -100f);

            return Math.Min(_touchBrake, Math.Min(controllerBrake, keyboardBrake));
        }

        private int ComputeClutch()
        {
            if (!_allowDrivingInput || _overlayInputBlocked)
                return 0;

            var controllerClutch = UseController ? GetPedalAxis(_clutch, _settings.ControllerClutchInvertMode) : 0;
            if (!UseKeyboard)
                return Math.Max(_touchClutch, controllerClutch);

            var keyboardClutch = (int)Math.Round(_simClutch * 100f);
            return Math.Max(_touchClutch, Math.Max(controllerClutch, keyboardClutch));
        }

        private int ApplySteeringDeadZone(int value)
        {
            var deadZone = _settings.ControllerSteeringDeadZone;
            if (deadZone < 1 || deadZone > 5)
                deadZone = 1;

            return Math.Abs(value) <= deadZone ? 0 : value;
        }

        private bool IsClutchKeyDown()
        {
            return IsKeyDown(_lastState, _kbClutch);
        }
    }
}



