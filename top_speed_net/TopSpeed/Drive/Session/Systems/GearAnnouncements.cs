using System;
using TopSpeed.Input;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Session.Systems
{
    internal static class GearAnnouncements
    {
        private const int FirstForwardGear = 1;

        public static bool ShouldAnnounceUserShift(ICar car, DriveInput input, int previousGear)
        {
            if (car == null)
                throw new ArgumentNullException(nameof(car));
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (car.Gear == previousGear)
                return false;
            if (!input.Intents.IsTriggered(DriveIntent.GearUp) && !input.Intents.IsTriggered(DriveIntent.GearDown))
                return false;

            if (car.ManualTransmission || car.ShiftOnDemandEnabled)
                return true;

            // In fully automatic mode, A/Z only selects Reverse/Neutral/Drive.
            // Do not speak automatic drivetrain upshifts like D2/D3/D4.
            return previousGear <= FirstForwardGear && car.Gear <= FirstForwardGear;
        }
    }
}
