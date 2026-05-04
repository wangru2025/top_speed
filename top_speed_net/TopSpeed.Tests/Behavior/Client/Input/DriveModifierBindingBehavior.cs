using TopSpeed.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class DriveModifierBindingBehaviorTests
{
    [Fact]
    public void LeftShiftBinding_DoesNotTriggerFromRightShift()
    {
        var input = new DriveInput(new DriveSettings { DeviceMode = InputDeviceMode.Keyboard });
        input.SetClutch(InputKey.LeftShift);

        var rightShift = new InputState();
        rightShift.Set(InputKey.RightShift, true);
        input.Run(rightShift, 0f);

        input.Intents.IsTriggered(DriveIntent.Clutch).Should().BeFalse();

        var leftShift = new InputState();
        leftShift.Set(InputKey.LeftShift, true);
        input.Run(leftShift, 0f);

        input.Intents.IsTriggered(DriveIntent.Clutch).Should().BeTrue();
    }

    [Fact]
    public void BothShiftBinding_TriggersFromEitherShiftKey()
    {
        var input = new DriveInput(new DriveSettings { DeviceMode = InputDeviceMode.Keyboard });
        input.SetClutch(InputKey.BothShift);

        var leftShift = new InputState();
        leftShift.Set(InputKey.LeftShift, true);
        input.Run(leftShift, 0f);

        input.Intents.IsTriggered(DriveIntent.Clutch).Should().BeTrue();

        var rightShift = new InputState();
        rightShift.Set(InputKey.RightShift, true);
        input.Run(rightShift, 0f);

        input.Intents.IsTriggered(DriveIntent.Clutch).Should().BeTrue();
    }

    [Fact]
    public void BothControlBinding_UsesPressSemanticsForEitherControlKey()
    {
        var input = new DriveInput(new DriveSettings { DeviceMode = InputDeviceMode.Keyboard });
        input.SetCurrentGear(InputKey.BothControl);

        input.Run(new InputState(), 0f);

        var rightControl = new InputState();
        rightControl.Set(InputKey.RightControl, true);
        input.Run(rightControl, 0f);

        input.Intents.IsTriggered(DriveIntent.CurrentGear).Should().BeTrue();

        input.Run(rightControl, 0f);

        input.Intents.IsTriggered(DriveIntent.CurrentGear).Should().BeFalse();
    }
}
