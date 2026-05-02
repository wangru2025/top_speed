using TopSpeed.Input;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class FinishLockInputController : ICarController
    {
        private readonly DriveInput _input;

        public FinishLockInputController(DriveInput input)
        {
            _input = input;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            return new CarControlIntent(
                _input.Intents.GetAxisPercent(DriveIntent.Steering),
                throttle: 0,
                brake: 0,
                clutch: _input.Intents.GetAxisPercent(DriveIntent.Clutch),
                horn: _input.Intents.IsTriggered(DriveIntent.Horn),
                gearUp: false,
                gearDown: false);
        }
    }
}

