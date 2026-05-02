using System.Reflection;
using System.Runtime.CompilerServices;
using TopSpeed.Vehicles;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ManualThrottleDriveBehaviorTests
{
    [Theory]
    [InlineData(0, true, false, 1f, false)]
    [InlineData(100, false, false, 0f, true)]
    [InlineData(100, true, true, 1f, false)]
    [InlineData(100, true, false, 0.05f, false)]
    [InlineData(100, true, false, 0.06f, true)]
    public void CanApplyThrottleDrive_ShouldRequireActualManualDrivelineCoupling(
        int thrust,
        bool manualTransmission,
        bool neutralGear,
        float drivelineCouplingFactor,
        bool expected)
    {
        var car = (Car)RuntimeHelpers.GetUninitializedObject(typeof(Car));
        SetField(car, "_thrust", thrust);
        SetField(car, "_manualTransmission", manualTransmission);
        SetField(car, "_gear", neutralGear ? 0 : 1);

        var method = typeof(Car).GetMethod("CanApplyThrottleDrive", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        ((bool)method!.Invoke(car, new object[] { drivelineCouplingFactor })!).Should().Be(expected);
    }

    private static void SetField<T>(Car car, string name, T value)
    {
        var field = typeof(Car).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        field.Should().NotBeNull();
        field!.SetValue(car, value);
    }
}
