using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Input
{
    public sealed class Gamepad : IDisposable
    {
        private const string LibraryName = SdlNativeLibrary.Name;
        private IntPtr _handle;

        private Gamepad(IntPtr handle)
        {
            _handle = handle;
        }

        public IntPtr Handle => _handle;
        public bool IsOpen => _handle != IntPtr.Zero;

        public static uint[] GetIds()
        {
            if (!Runtime.IsAvailable)
                return Array.Empty<uint>();

            var pointer = SDL_GetGamepads(out var count);
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

        public static bool IsGamepad(uint instanceId)
        {
            return Runtime.IsAvailable && SDL_IsGamepad(instanceId);
        }

        public static string? GetNameForId(uint instanceId)
        {
            return Utf8.FromNative(SDL_GetGamepadNameForID(instanceId));
        }

        public static string? GetPathForId(uint instanceId)
        {
            return Utf8.FromNative(SDL_GetGamepadPathForID(instanceId));
        }

        public static int GetPlayerIndexForId(uint instanceId)
        {
            return SDL_GetGamepadPlayerIndexForID(instanceId);
        }

        public static ushort GetVendorForId(uint instanceId)
        {
            return SDL_GetGamepadVendorForID(instanceId);
        }

        public static ushort GetProductForId(uint instanceId)
        {
            return SDL_GetGamepadProductForID(instanceId);
        }

        public static ushort GetProductVersionForId(uint instanceId)
        {
            return SDL_GetGamepadProductVersionForID(instanceId);
        }

        public static GamepadType GetTypeForId(uint instanceId)
        {
            return SDL_GetGamepadTypeForID(instanceId);
        }

        public static DeviceMetadata GetMetadataForId(uint instanceId)
        {
            return new DeviceMetadata(
                instanceId,
                true,
                GetNameForId(instanceId),
                GetPathForId(instanceId),
                Joystick.GetGuidForId(instanceId),
                Joystick.GetTypeForId(instanceId),
                GetTypeForId(instanceId),
                GetPlayerIndexForId(instanceId),
                GetVendorForId(instanceId),
                GetProductForId(instanceId),
                GetProductVersionForId(instanceId),
                0,
                string.Empty);
        }

        public static Gamepad? Open(uint instanceId)
        {
            if (!Runtime.IsAvailable)
                return null;

            var handle = SDL_OpenGamepad(instanceId);
            return handle == IntPtr.Zero ? null : new Gamepad(handle);
        }

        public string? Name => Utf8.FromNative(SDL_GetGamepadName(_handle));
        public string? Path => Utf8.FromNative(SDL_GetGamepadPath(_handle));
        public GamepadType Type => SDL_GetGamepadType(_handle);
        public uint InstanceId => SDL_GetGamepadID(_handle);
        public int PlayerIndex => SDL_GetGamepadPlayerIndex(_handle);
        public ushort VendorId => SDL_GetGamepadVendor(_handle);
        public ushort ProductId => SDL_GetGamepadProduct(_handle);
        public ushort ProductVersion => SDL_GetGamepadProductVersion(_handle);
        public ushort FirmwareVersion => SDL_GetGamepadFirmwareVersion(_handle);
        public string? Serial => Utf8.FromNative(SDL_GetGamepadSerial(_handle));
        public PowerInfo PowerInfo => new PowerInfo(SDL_GetGamepadPowerInfo(_handle, out var percent), percent);
        public DeviceMetadata Metadata => new DeviceMetadata(
            InstanceId,
            true,
            Name,
            Path,
            Joystick.GetGuidForId(InstanceId),
            Joystick.GetTypeForId(InstanceId),
            Type,
            PlayerIndex,
            VendorId,
            ProductId,
            ProductVersion,
            FirmwareVersion,
            Serial);
        public short GetAxis(GamepadAxis axis) => SDL_GetGamepadAxis(_handle, axis);
        public bool GetButton(GamepadButton button) => SDL_GetGamepadButton(_handle, button);
        public bool Rumble(ushort low, ushort high, int durationMs) => SDL_RumbleGamepad(_handle, low, high, (uint)durationMs);
        public bool RumbleTriggers(ushort left, ushort right, int durationMs) => SDL_RumbleGamepadTriggers(_handle, left, right, (uint)durationMs);
        public bool SetLed(byte red, byte green, byte blue) => SDL_SetGamepadLED(_handle, red, green, blue);

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            SDL_CloseGamepad(_handle);
            _handle = IntPtr.Zero;
        }

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepads", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetGamepads(out int count);

        [DllImport(LibraryName, EntryPoint = "SDL_IsGamepad", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_IsGamepad(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadNameForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetGamepadNameForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadPathForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetGamepadPathForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadPlayerIndexForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetGamepadPlayerIndexForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadVendorForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetGamepadVendorForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadProductForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetGamepadProductForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadProductVersionForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetGamepadProductVersionForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadTypeForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern GamepadType SDL_GetGamepadTypeForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_OpenGamepad", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_OpenGamepad(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadName", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetGamepadName(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadPath", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetGamepadPath(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadPlayerIndex", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetGamepadPlayerIndex(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadVendor", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetGamepadVendor(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadProduct", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetGamepadProduct(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadProductVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetGamepadProductVersion(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadFirmwareVersion", CallingConvention = CallingConvention.Cdecl)]
        private static extern ushort SDL_GetGamepadFirmwareVersion(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadSerial", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetGamepadSerial(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadType", CallingConvention = CallingConvention.Cdecl)]
        private static extern GamepadType SDL_GetGamepadType(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadID", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetGamepadID(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadAxis", CallingConvention = CallingConvention.Cdecl)]
        private static extern short SDL_GetGamepadAxis(IntPtr gamepad, GamepadAxis axis);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadButton", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_GetGamepadButton(IntPtr gamepad, GamepadButton button);

        [DllImport(LibraryName, EntryPoint = "SDL_RumbleGamepad", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_RumbleGamepad(IntPtr gamepad, ushort low, ushort high, uint durationMs);

        [DllImport(LibraryName, EntryPoint = "SDL_RumbleGamepadTriggers", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_RumbleGamepadTriggers(IntPtr gamepad, ushort left, ushort right, uint durationMs);

        [DllImport(LibraryName, EntryPoint = "SDL_GetGamepadPowerInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern PowerState SDL_GetGamepadPowerInfo(IntPtr gamepad, out int percent);

        [DllImport(LibraryName, EntryPoint = "SDL_SetGamepadLED", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_SetGamepadLED(IntPtr gamepad, byte red, byte green, byte blue);

        [DllImport(LibraryName, EntryPoint = "SDL_CloseGamepad", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_CloseGamepad(IntPtr gamepad);

        [DllImport(LibraryName, EntryPoint = "SDL_free", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_Free(IntPtr memory);
    }
}
