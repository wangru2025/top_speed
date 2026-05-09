using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Input
{
    public sealed class Joystick : IDisposable
    {
        private const string LibraryName = SdlNativeLibrary.Name;
        private IntPtr _handle;

        private Joystick(IntPtr handle)
        {
            _handle = handle;
        }

        public IntPtr Handle => _handle;
        public bool IsOpen => _handle != IntPtr.Zero;

        public static uint[] GetIds()
        {
            if (!Runtime.IsAvailable)
                return Array.Empty<uint>();

            var pointer = SDL_GetJoysticks(out var count);
            try
            {
                return Memory.ReadArray<uint>(pointer, count);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                    SDL_Free(pointer);
            }
        }

        public static string? GetNameForId(uint instanceId)
        {
            return Utf8.FromNative(SDL_GetJoystickNameForID(instanceId));
        }

        public static string? GetPathForId(uint instanceId)
        {
            return Utf8.FromNative(SDL_GetJoystickPathForID(instanceId));
        }

        public static Guid GetGuidForId(uint instanceId)
        {
            return SDL_GetJoystickGUIDForID(instanceId).ToGuid();
        }

        public static int GetPlayerIndexForId(uint instanceId)
        {
            return SDL_GetJoystickPlayerIndexForID(instanceId);
        }

        public static ushort GetVendorForId(uint instanceId)
        {
            return SDL_GetJoystickVendorForID(instanceId);
        }

        public static ushort GetProductForId(uint instanceId)
        {
            return SDL_GetJoystickProductForID(instanceId);
        }

        public static ushort GetProductVersionForId(uint instanceId)
        {
            return SDL_GetJoystickProductVersionForID(instanceId);
        }

        public static JoystickType GetTypeForId(uint instanceId)
        {
            return SDL_GetJoystickTypeForID(instanceId);
        }

        public static DeviceMetadata GetMetadataForId(uint instanceId)
        {
            return new DeviceMetadata(
                instanceId,
                false,
                GetNameForId(instanceId),
                GetPathForId(instanceId),
                GetGuidForId(instanceId),
                GetTypeForId(instanceId),
                GamepadType.Unknown,
                GetPlayerIndexForId(instanceId),
                GetVendorForId(instanceId),
                GetProductForId(instanceId),
                GetProductVersionForId(instanceId),
                0,
                string.Empty);
        }

        public static Joystick? Open(uint instanceId)
        {
            if (!Runtime.IsAvailable)
                return null;

            var handle = SDL_OpenJoystick(instanceId);
            return handle == IntPtr.Zero ? null : new Joystick(handle);
        }

        public string? Name => Utf8.FromNative(SDL_GetJoystickName(_handle));
        public string? Path => Utf8.FromNative(SDL_GetJoystickPath(_handle));
        public Guid Guid => SDL_GetJoystickGUID(_handle).ToGuid();
        public JoystickType Type => SDL_GetJoystickType(_handle);
        public JoystickConnectionState ConnectionState => SDL_GetJoystickConnectionState(_handle);
        public int AxisCount => SDL_GetNumJoystickAxes(_handle);
        public int HatCount => SDL_GetNumJoystickHats(_handle);
        public int ButtonCount => SDL_GetNumJoystickButtons(_handle);
        public uint InstanceId => SDL_GetJoystickID(_handle);
        public int PlayerIndex => SDL_GetJoystickPlayerIndex(_handle);
        public ushort VendorId => SDL_GetJoystickVendor(_handle);
        public ushort ProductId => SDL_GetJoystickProduct(_handle);
        public ushort ProductVersion => SDL_GetJoystickProductVersion(_handle);
        public ushort FirmwareVersion => SDL_GetJoystickFirmwareVersion(_handle);
        public string? Serial => Utf8.FromNative(SDL_GetJoystickSerial(_handle));
        public PowerInfo PowerInfo => new PowerInfo(SDL_GetJoystickPowerInfo(_handle, out var percent), percent);
        public DeviceMetadata Metadata => new DeviceMetadata(
            InstanceId,
            false,
            Name,
            Path,
            Guid,
            Type,
            GamepadType.Unknown,
            PlayerIndex,
            VendorId,
            ProductId,
            ProductVersion,
            FirmwareVersion,
            Serial);

        public short GetAxis(int axis) => SDL_GetJoystickAxis(_handle, axis);
        public JoystickHat GetHat(int hat) => SDL_GetJoystickHat(_handle, hat);
        public bool GetButton(int button) => SDL_GetJoystickButton(_handle, button);
        public bool Rumble(ushort low, ushort high, int durationMs) => SDL_RumbleJoystick(_handle, (short)low, (short)high, durationMs);
        public bool RumbleTriggers(ushort left, ushort right, int durationMs) => SDL_RumbleJoystickTriggers(_handle, (short)left, (short)right, durationMs);
        public bool SetLed(byte red, byte green, byte blue) => SDL_SetJoystickLED(_handle, red, green, blue);

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            SDL_CloseJoystick(_handle);
            _handle = IntPtr.Zero;
        }

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoysticks", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetJoysticks(out int count);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickNameForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetJoystickNameForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickPathForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetJoystickPathForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickPlayerIndexForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetJoystickPlayerIndexForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickGUIDForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern GuidValue SDL_GetJoystickGUIDForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickVendorForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetJoystickVendorForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickProductForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetJoystickProductForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickProductVersionForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetJoystickProductVersionForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickTypeForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern JoystickType SDL_GetJoystickTypeForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_OpenJoystick", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_OpenJoystick(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickName", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetJoystickName(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickPath", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetJoystickPath(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickPlayerIndex", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetJoystickPlayerIndex(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickGUID", CallingConvention = CallingConvention.Cdecl)]
        private static extern GuidValue SDL_GetJoystickGUID(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickVendor", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetJoystickVendor(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickProduct", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetJoystickProduct(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickProductVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetJoystickProductVersion(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickFirmwareVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetJoystickFirmwareVersion(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickSerial", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetJoystickSerial(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickType", CallingConvention = CallingConvention.Cdecl)]
        private static extern JoystickType SDL_GetJoystickType(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickConnectionState", CallingConvention = CallingConvention.Cdecl)]
        private static extern JoystickConnectionState SDL_GetJoystickConnectionState(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetNumJoystickAxes", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetNumJoystickAxes(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetNumJoystickHats", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetNumJoystickHats(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetNumJoystickButtons", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetNumJoystickButtons(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickID", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetJoystickID(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickAxis", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SDL_GetJoystickAxis(IntPtr joystick, int axis);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickHat", CallingConvention = CallingConvention.Cdecl)]
        private static extern JoystickHat SDL_GetJoystickHat(IntPtr joystick, int hat);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickButton", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_GetJoystickButton(IntPtr joystick, int button);

        [DllImport(LibraryName, EntryPoint = "SDL_RumbleJoystick", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_RumbleJoystick(IntPtr joystick, short low, short high, int durationMs);

        [DllImport(LibraryName, EntryPoint = "SDL_RumbleJoystickTriggers", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_RumbleJoystickTriggers(IntPtr joystick, short left, short right, int durationMs);

        [DllImport(LibraryName, EntryPoint = "SDL_GetJoystickPowerInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern PowerState SDL_GetJoystickPowerInfo(IntPtr joystick, out int percent);

        [DllImport(LibraryName, EntryPoint = "SDL_SetJoystickLED", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_SetJoystickLED(IntPtr joystick, byte red, byte green, byte blue);

        [DllImport(LibraryName, EntryPoint = "SDL_CloseJoystick", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_CloseJoystick(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_free", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_Free(IntPtr memory);
    }
}
