namespace TS.Sdl
{
    internal static class SdlNativeLibrary
    {
#if TS_SDL_IOS_FRAMEWORK
        internal const string Name = "@rpath/SDL3.framework/SDL3";
#else
        internal const string Name = "SDL3";
#endif
    }
}
