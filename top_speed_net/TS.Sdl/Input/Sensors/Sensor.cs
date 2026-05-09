using System;
using System.Runtime.InteropServices;
using TS.Sdl.Interop;

namespace TS.Sdl.Input
{
    public sealed class Sensor : IDisposable
    {
        private const string LibraryName = SdlNativeLibrary.Name;
        private IntPtr _handle;

        private Sensor(IntPtr handle)
        {
            _handle = handle;
        }

        public bool IsOpen => _handle != IntPtr.Zero;
        public uint InstanceId => SDL_GetSensorID(_handle);
        public SensorType Type => SDL_GetSensorType(_handle);
        public int NonPortableType => SDL_GetSensorNonPortableType(_handle);
        public uint PropertiesId => SDL_GetSensorProperties(_handle);
        public string? Name => Utf8.FromNative(SDL_GetSensorName(_handle));

        public static uint[] GetIds()
        {
            if (!Runtime.IsAvailable)
                return Array.Empty<uint>();

            var pointer = SDL_GetSensors(out var count);
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
            return Utf8.FromNative(SDL_GetSensorNameForID(instanceId));
        }

        public static SensorType GetTypeForId(uint instanceId)
        {
            return SDL_GetSensorTypeForID(instanceId);
        }

        public static int GetNonPortableTypeForId(uint instanceId)
        {
            return SDL_GetSensorNonPortableTypeForID(instanceId);
        }

        public static IntPtr GetHandleForId(uint instanceId)
        {
            if (!Runtime.IsAvailable)
                return IntPtr.Zero;

            return SDL_GetSensorFromID(instanceId);
        }

        public static void Update()
        {
            if (!Runtime.IsAvailable)
                return;

            SDL_UpdateSensors();
        }

        public static Sensor? Open(uint instanceId)
        {
            if (!Runtime.IsAvailable)
                return null;

            var handle = SDL_OpenSensor(instanceId);
            return handle == IntPtr.Zero ? null : new Sensor(handle);
        }

        public bool TryGetData(float[] values)
        {
            if (!IsOpen || values == null || values.Length == 0)
                return false;

            return SDL_GetSensorData(_handle, values, values.Length);
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            SDL_CloseSensor(_handle);
            _handle = IntPtr.Zero;
        }

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensors", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetSensors(out int count);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorNameForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetSensorNameForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorTypeForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern SensorType SDL_GetSensorTypeForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorNonPortableTypeForID", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetSensorNonPortableTypeForID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_OpenSensor", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_OpenSensor(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorFromID", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetSensorFromID(uint instanceId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorID", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetSensorID(IntPtr sensor);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorType", CallingConvention = CallingConvention.Cdecl)]
        private static extern SensorType SDL_GetSensorType(IntPtr sensor);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorNonPortableType", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetSensorNonPortableType(IntPtr sensor);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorProperties", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint SDL_GetSensorProperties(IntPtr sensor);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorName", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GetSensorName(IntPtr sensor);

        [DllImport(LibraryName, EntryPoint = "SDL_GetSensorData", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_GetSensorData(IntPtr sensor, [Out] float[] data, int numValues);

        [DllImport(LibraryName, EntryPoint = "SDL_CloseSensor", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_CloseSensor(IntPtr sensor);

        [DllImport(LibraryName, EntryPoint = "SDL_UpdateSensors", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_UpdateSensors();

        [DllImport(LibraryName, EntryPoint = "SDL_free", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_Free(IntPtr memory);
    }
}
