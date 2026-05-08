using System;
using CoreMotion;
using TopSpeed.Runtime;

namespace TopSpeed.iOS;

internal sealed class IosMotionSteeringSource : IMotionSteeringSource
{
    private readonly object _sync = new object();
    private readonly CMMotionManager _motionManager;
    private bool _hasNeutral;
    private bool _hasReading;
    private bool _disposed;
    private float _neutralRoll;
    private float _currentRoll;

    public IosMotionSteeringSource()
    {
        _motionManager = new CMMotionManager();
        if (!_motionManager.DeviceMotionAvailable)
            return;

        _motionManager.DeviceMotionUpdateInterval = 1d / 60d;
        try
        {
            _motionManager.StartDeviceMotionUpdates(ResolveReferenceFrame());
        }
        catch
        {
            try
            {
                _motionManager.StartDeviceMotionUpdates();
            }
            catch
            {
                // Device-motion updates are unavailable on this runtime/device.
            }
        }
    }

    public bool IsAvailable => !_disposed && _motionManager.DeviceMotionAvailable;

    public void Recenter()
    {
        lock (_sync)
        {
            _hasNeutral = false;
            _hasReading = false;
            _currentRoll = 0f;
        }
    }

    public bool TryGetSteeringAngleRadians(out float angleRadians)
    {
        if (_disposed || !_motionManager.DeviceMotionAvailable || !_motionManager.DeviceMotionActive)
        {
            angleRadians = 0f;
            return false;
        }

        var motion = _motionManager.DeviceMotion;
        if (motion == null)
        {
            angleRadians = 0f;
            return false;
        }

        var roll = (float)motion.Attitude.Roll;
        lock (_sync)
        {
            if (!_hasNeutral)
            {
                _neutralRoll = roll;
                _hasNeutral = true;
            }

            _currentRoll = WrapAngle(roll - _neutralRoll);
            _hasReading = true;
            angleRadians = _currentRoll;
            return _hasReading;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_motionManager.DeviceMotionActive)
            _motionManager.StopDeviceMotionUpdates();
        _motionManager.Dispose();
    }

    private static float WrapAngle(float value)
    {
        var twoPi = 2f * (float)Math.PI;
        while (value > Math.PI)
            value -= twoPi;
        while (value < -Math.PI)
            value += twoPi;
        return value;
    }

    private static CMAttitudeReferenceFrame ResolveReferenceFrame()
    {
        var available = CMMotionManager.AvailableAttitudeReferenceFrames;
        if ((available & CMAttitudeReferenceFrame.XArbitraryZVertical) != 0)
            return CMAttitudeReferenceFrame.XArbitraryZVertical;
        if ((available & CMAttitudeReferenceFrame.XArbitraryCorrectedZVertical) != 0)
            return CMAttitudeReferenceFrame.XArbitraryCorrectedZVertical;
        if ((available & CMAttitudeReferenceFrame.XMagneticNorthZVertical) != 0)
            return CMAttitudeReferenceFrame.XMagneticNorthZVertical;
        if ((available & CMAttitudeReferenceFrame.XTrueNorthZVertical) != 0)
            return CMAttitudeReferenceFrame.XTrueNorthZVertical;

        return CMAttitudeReferenceFrame.XArbitraryZVertical;
    }
}
