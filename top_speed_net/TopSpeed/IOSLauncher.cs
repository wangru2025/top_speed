namespace TopSpeed
{
    public static class IOSLauncher
    {
        public static void SetAssetRoot(string? path)
        {
            MobileLauncher.SetAssetRoot(path);
        }

        public static void Run()
        {
            MobileLauncher.Run();
        }

        public static void RequestClose()
        {
            MobileLauncher.RequestClose();
        }
    }
}
