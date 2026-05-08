using System;
using System.Runtime.InteropServices;

namespace TopSpeed.Speech.Prism
{
    internal static class Native
    {
        private static readonly IMethods Methods = CreateMethods();

        private static IMethods CreateMethods()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")))
                return new AndroidMethods();

            if (IsIOS())
                return new MacMethods();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsMethods();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxMethods();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new MacMethods();

            throw new PlatformNotSupportedException("Prism is not configured for this platform.");
        }

        private static bool IsIOS()
        {
#if NET10_0_OR_GREATER
            return OperatingSystem.IsIOS();
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS"));
#endif
        }

        public static Config ConfigInit() => Methods.ConfigInit();
        public static IntPtr Init(ref Config config) => Methods.Init(ref config);
        public static void Shutdown(IntPtr context) => Methods.Shutdown(context);
        public static int RegistryCount(IntPtr context) => Methods.RegistryCount(context);
        public static ulong RegistryIdAt(IntPtr context, int index) => Methods.RegistryIdAt(context, index);
        public static string? RegistryName(IntPtr context, ulong id) => Methods.RegistryName(context, id);
        public static int RegistryPriority(IntPtr context, ulong id) => Methods.RegistryPriority(context, id);
        public static bool RegistryExists(IntPtr context, ulong id) => Methods.RegistryExists(context, id);
        public static ulong RegistryId(IntPtr context, string name) => Methods.RegistryId(context, name);
        public static IntPtr Create(IntPtr context, ulong id) => Methods.Create(context, id);
        public static IntPtr CreateBest(IntPtr context) => Methods.CreateBest(context);
        public static IntPtr Acquire(IntPtr context, ulong id) => Methods.Acquire(context, id);
        public static IntPtr AcquireBest(IntPtr context) => Methods.AcquireBest(context);
        public static void FreeBackend(IntPtr backend) => Methods.FreeBackend(backend);
        public static string? BackendName(IntPtr backend) => Methods.BackendName(backend);
        public static Features BackendFeatures(IntPtr backend) => Methods.BackendFeatures(backend);
        public static Error InitializeBackend(IntPtr backend) => Methods.InitializeBackend(backend);
        public static Error Speak(IntPtr backend, string text, bool interrupt) => Methods.Speak(backend, text, interrupt);
        public static Error SpeakToMemory(IntPtr backend, string text, MemoryAudioCallback callback) => Methods.SpeakToMemory(backend, text, callback);
        public static Error Braille(IntPtr backend, string text) => Methods.Braille(backend, text);
        public static Error Output(IntPtr backend, string text, bool interrupt) => Methods.Output(backend, text, interrupt);
        public static Error Stop(IntPtr backend) => Methods.Stop(backend);
        public static Error Pause(IntPtr backend) => Methods.Pause(backend);
        public static Error Resume(IntPtr backend) => Methods.Resume(backend);
        public static Error IsSpeaking(IntPtr backend, out bool speaking) => Methods.IsSpeaking(backend, out speaking);
        public static Error SetVolume(IntPtr backend, float volume) => Methods.SetVolume(backend, volume);
        public static Error GetVolume(IntPtr backend, out float volume) => Methods.GetVolume(backend, out volume);
        public static Error SetRate(IntPtr backend, float rate) => Methods.SetRate(backend, rate);
        public static Error GetRate(IntPtr backend, out float rate) => Methods.GetRate(backend, out rate);
        public static Error SetPitch(IntPtr backend, float pitch) => Methods.SetPitch(backend, pitch);
        public static Error GetPitch(IntPtr backend, out float pitch) => Methods.GetPitch(backend, out pitch);
        public static Error RefreshVoices(IntPtr backend) => Methods.RefreshVoices(backend);
        public static Error CountVoices(IntPtr backend, out int count) => Methods.CountVoices(backend, out count);
        public static Error GetVoiceName(IntPtr backend, int voiceIndex, out string? name) => Methods.GetVoiceName(backend, voiceIndex, out name);
        public static Error GetVoiceLanguage(IntPtr backend, int voiceIndex, out string? language) => Methods.GetVoiceLanguage(backend, voiceIndex, out language);
        public static Error SetVoice(IntPtr backend, int voiceIndex) => Methods.SetVoice(backend, voiceIndex);
        public static Error GetVoice(IntPtr backend, out int voiceIndex) => Methods.GetVoice(backend, out voiceIndex);
        public static Error GetChannels(IntPtr backend, out int channels) => Methods.GetChannels(backend, out channels);
        public static Error GetSampleRate(IntPtr backend, out int sampleRate) => Methods.GetSampleRate(backend, out sampleRate);
        public static Error GetBitDepth(IntPtr backend, out int bitDepth) => Methods.GetBitDepth(backend, out bitDepth);
        public static string? ErrorString(Error error) => Methods.ErrorString(error);
    }
}
