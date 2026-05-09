using System;
using System.Runtime.InteropServices;

namespace TS.Sdl.Input
{
    public sealed partial class HapticDevice : IDisposable
    {
        private const string LibraryName = SdlNativeLibrary.Name;
        private IntPtr _handle;
        private bool _rumbleInitialized;

        private HapticDevice(IntPtr handle)
        {
            _handle = handle;
        }

        public bool IsOpen => _handle != IntPtr.Zero;
        public HapticFeatures Features => IsOpen ? SDL_GetHapticFeatures(_handle) : HapticFeatures.None;
        public int AxisCount => IsOpen ? SDL_GetNumHapticAxes(_handle) : 0;
        public int MaxEffects => IsOpen ? SDL_GetMaxHapticEffects(_handle) : 0;
        public int MaxPlayingEffects => IsOpen ? SDL_GetMaxHapticEffectsPlaying(_handle) : 0;

        public static HapticDevice? OpenFromJoystick(Joystick joystick)
        {
            if (joystick == null || joystick.Handle == IntPtr.Zero)
                return null;

            if (!SDL_IsJoystickHaptic(joystick.Handle))
                return null;

            var handle = SDL_OpenHapticFromJoystick(joystick.Handle);
            return handle == IntPtr.Zero ? null : new HapticDevice(handle);
        }

        public bool SupportsFeature(HapticFeatures feature)
        {
            return IsOpen && (Features & feature) == feature;
        }

        public bool SupportsRumble()
        {
            return IsOpen && SDL_HapticRumbleSupported(_handle);
        }

        public bool Rumble(float strength, uint length)
        {
            if (!IsOpen)
                return false;

            if (!_rumbleInitialized)
            {
                _rumbleInitialized = SDL_InitHapticRumble(_handle);
                if (!_rumbleInitialized)
                    return false;
            }

            return SDL_PlayHapticRumble(_handle, strength, length);
        }

        public bool StopRumble()
        {
            return IsOpen && SDL_StopHapticRumble(_handle);
        }

        public bool SetGain(int gain)
        {
            return IsOpen && SDL_SetHapticGain(_handle, gain);
        }

        public bool SetAutoCenter(int value)
        {
            return IsOpen && SDL_SetHapticAutocenter(_handle, value);
        }

        public bool Pause()
        {
            return IsOpen && SDL_PauseHaptic(_handle);
        }

        public bool Resume()
        {
            return IsOpen && SDL_ResumeHaptic(_handle);
        }

        public bool StopAllEffects()
        {
            return IsOpen && SDL_StopHapticEffects(_handle);
        }

        public bool SupportsEffect(in HapticEffect effect)
        {
            if (!IsOpen)
                return false;

            var candidate = effect;
            return SDL_HapticEffectSupported(_handle, ref candidate);
        }

        public HapticEffectHandle? CreateEffect(in HapticEffect effect)
        {
            if (!IsOpen)
                return null;

            var candidate = effect;
            var id = SDL_CreateHapticEffect(_handle, ref candidate);
            return id < 0 ? null : new HapticEffectHandle(this, id);
        }

        public bool UpdateEffect(HapticEffectHandle handle, in HapticEffect effect)
        {
            if (!IsOpen || handle == null || !handle.BelongsTo(this) || !handle.IsValid)
                return false;

            var candidate = effect;
            return SDL_UpdateHapticEffect(_handle, handle.Id, ref candidate);
        }

        public bool RunEffect(HapticEffectHandle handle, uint iterations)
        {
            return IsOpen
                && handle != null
                && handle.BelongsTo(this)
                && handle.IsValid
                && SDL_RunHapticEffect(_handle, handle.Id, iterations);
        }

        public bool StopEffect(HapticEffectHandle handle)
        {
            return IsOpen
                && handle != null
                && handle.BelongsTo(this)
                && handle.IsValid
                && SDL_StopHapticEffect(_handle, handle.Id);
        }

        public bool DestroyEffect(HapticEffectHandle handle)
        {
            if (!IsOpen || handle == null || !handle.BelongsTo(this) || !handle.IsValid)
                return false;

            SDL_DestroyHapticEffect(_handle, handle.Id);
            handle.Invalidate();
            return true;
        }

        public bool IsEffectPlaying(HapticEffectHandle handle)
        {
            return IsOpen
                && handle != null
                && handle.BelongsTo(this)
                && handle.IsValid
                && SDL_GetHapticEffectStatus(_handle, handle.Id);
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            SDL_CloseHaptic(_handle);
            _handle = IntPtr.Zero;
            _rumbleInitialized = false;
        }

        [DllImport(LibraryName, EntryPoint = "SDL_IsJoystickHaptic", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_IsJoystickHaptic(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_OpenHapticFromJoystick", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_OpenHapticFromJoystick(IntPtr joystick);

        [DllImport(LibraryName, EntryPoint = "SDL_GetHapticFeatures", CallingConvention = CallingConvention.Cdecl)]
        private static extern HapticFeatures SDL_GetHapticFeatures(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_GetNumHapticAxes", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetNumHapticAxes(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_GetMaxHapticEffects", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetMaxHapticEffects(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_GetMaxHapticEffectsPlaying", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_GetMaxHapticEffectsPlaying(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_HapticRumbleSupported", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_HapticRumbleSupported(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_InitHapticRumble", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_InitHapticRumble(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_PlayHapticRumble", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_PlayHapticRumble(IntPtr haptic, float strength, uint length);

        [DllImport(LibraryName, EntryPoint = "SDL_StopHapticRumble", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_StopHapticRumble(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_SetHapticGain", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_SetHapticGain(IntPtr haptic, int gain);

        [DllImport(LibraryName, EntryPoint = "SDL_SetHapticAutocenter", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_SetHapticAutocenter(IntPtr haptic, int value);

        [DllImport(LibraryName, EntryPoint = "SDL_PauseHaptic", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_PauseHaptic(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_ResumeHaptic", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_ResumeHaptic(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_StopHapticEffects", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_StopHapticEffects(IntPtr haptic);

        [DllImport(LibraryName, EntryPoint = "SDL_HapticEffectSupported", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_HapticEffectSupported(IntPtr haptic, ref HapticEffect effect);

        [DllImport(LibraryName, EntryPoint = "SDL_CreateHapticEffect", CallingConvention = CallingConvention.Cdecl)]
        private static extern int SDL_CreateHapticEffect(IntPtr haptic, ref HapticEffect effect);

        [DllImport(LibraryName, EntryPoint = "SDL_UpdateHapticEffect", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_UpdateHapticEffect(IntPtr haptic, int effectId, ref HapticEffect effect);

        [DllImport(LibraryName, EntryPoint = "SDL_RunHapticEffect", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_RunHapticEffect(IntPtr haptic, int effectId, uint iterations);

        [DllImport(LibraryName, EntryPoint = "SDL_StopHapticEffect", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_StopHapticEffect(IntPtr haptic, int effectId);

        [DllImport(LibraryName, EntryPoint = "SDL_DestroyHapticEffect", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_DestroyHapticEffect(IntPtr haptic, int effectId);

        [DllImport(LibraryName, EntryPoint = "SDL_GetHapticEffectStatus", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SDL_GetHapticEffectStatus(IntPtr haptic, int effectId);

        [DllImport(LibraryName, EntryPoint = "SDL_CloseHaptic", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_CloseHaptic(IntPtr haptic);
    }
}
