using System;
using System.Collections.Generic;
using TopSpeed.Protocol;
using TopSpeed.Runtime;

namespace TopSpeed.Core.Updates
{
    internal sealed class UpdateSource
    {
        public UpdateSource(string infoUrl, string latestReleaseApiUrl)
        {
            InfoUrl = infoUrl ?? throw new ArgumentNullException(nameof(infoUrl));
            LatestReleaseApiUrl = latestReleaseApiUrl ?? throw new ArgumentNullException(nameof(latestReleaseApiUrl));
        }

        public string InfoUrl { get; }
        public string LatestReleaseApiUrl { get; }
    }

    internal sealed class UpdateConfig
    {
        private const string MirrorBaseUrl = "https://ts.wangru.net/top_speed";
        private const string RepoOwner = "wangru2025";
        private const string RepoName = "top_speed";
        private const string RepoBranch = "china-release";

        public UpdateConfig(
            string infoUrl,
            string latestReleaseApiUrl,
            string assetTemplate,
            string runtimeAssetTag,
            string updaterEntryName,
            string gameEntryName)
            : this(
                new[]
                {
                    new UpdateSource(infoUrl, latestReleaseApiUrl)
                },
                assetTemplate,
                runtimeAssetTag,
                updaterEntryName,
                gameEntryName)
        {
        }

        public UpdateConfig(
            IReadOnlyList<UpdateSource> sources,
            string assetTemplate,
            string runtimeAssetTag,
            string updaterEntryName,
            string gameEntryName)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            if (sources.Count == 0)
                throw new ArgumentException("At least one update source is required.", nameof(sources));

            Sources = sources;
            AssetTemplate = assetTemplate ?? throw new ArgumentNullException(nameof(assetTemplate));
            RuntimeAssetTag = runtimeAssetTag ?? throw new ArgumentNullException(nameof(runtimeAssetTag));
            UpdaterEntryName = updaterEntryName ?? throw new ArgumentNullException(nameof(updaterEntryName));
            GameEntryName = gameEntryName ?? throw new ArgumentNullException(nameof(gameEntryName));
        }

        public IReadOnlyList<UpdateSource> Sources { get; }
        public string InfoUrl => Sources[0].InfoUrl;
        public string LatestReleaseApiUrl => Sources[0].LatestReleaseApiUrl;
        public string AssetTemplate { get; }
        public string RuntimeAssetTag { get; }
        public string UpdaterEntryName { get; }
        public string GameEntryName { get; }

        public static UpdateConfig Default { get; } = CreateDefault();

        public static GameVersion CurrentVersion =>
            new GameVersion(
                ReleaseVersionInfo.ClientYear,
                ReleaseVersionInfo.ClientMonth,
                ReleaseVersionInfo.ClientDay,
                ReleaseVersionInfo.ClientRevision);

        public string BuildExpectedAssetName(string version)
        {
            return AssetTemplate
                .Replace("{runtime}", RuntimeAssetTag)
                .Replace("{version}", version ?? string.Empty)
                .Replace("{ext}", ResolvePackageExtension(RuntimeAssetTag));
        }

        private static UpdateConfig CreateDefault()
        {
            var runtimeAssetTag = ResolveRuntimeAssetTag();
            return new UpdateConfig(
                new[]
                {
                    new UpdateSource(
                        $"{MirrorBaseUrl}/info.json",
                        $"{MirrorBaseUrl}/releases/latest"),
                    new UpdateSource(
                        $"https://raw.githubusercontent.com/{RepoOwner}/{RepoName}/{RepoBranch}/info.json",
                        $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest")
                },
                "TopSpeed-{runtime}-Release-v-{version}{ext}",
                runtimeAssetTag,
                "Updater",
                "TopSpeed");
        }

        private static string ResolveRuntimeAssetTag()
        {
            try
            {
                return RuntimeAssetResolver.DetectClientRuntimeAssetTag();
            }
            catch (PlatformNotSupportedException)
            {
                return string.Empty;
            }
        }

        private static string ResolvePackageExtension(string runtimeAssetTag)
        {
            return runtimeAssetTag.StartsWith("android", StringComparison.OrdinalIgnoreCase)
                ? ".apk"
                : ".zip";
        }
    }
}

