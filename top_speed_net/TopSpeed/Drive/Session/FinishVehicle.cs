using System;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Control;

namespace TopSpeed.Drive.Session
{
    internal static class FinishVehicle
    {
        public static void Apply(ICar car, ICarController controller)
        {
            if (car == null)
                throw new ArgumentNullException(nameof(car));
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            car.ManualTransmission = false;
            car.SetOverrideController(controller);
            car.SetNeutralGear();
            car.Quiet();
            car.ShutdownEngine();
            car.StopMotionImmediately();
        }
    }
}
