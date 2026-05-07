using System;
using System.Collections.Generic;

namespace TopSpeed.Core.Updates
{
    internal sealed class UpdateInfo
    {
        public string VersionText { get; set; } = string.Empty;
        public GameVersion Version { get; set; }
        public IReadOnlyList<string> Changes { get; set; } = Array.Empty<string>();
        public string DownloadUrl { get; set; } = string.Empty;
        public IReadOnlyList<string> DownloadUrls { get; set; } = Array.Empty<string>();
        public long AssetSizeBytes { get; set; }
    }

    internal sealed class UpdateCheckResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public UpdateInfo? Update { get; set; }
    }

    internal sealed class LatestChangesResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string VersionText { get; set; } = string.Empty;
        public IReadOnlyList<string> Changes { get; set; } = Array.Empty<string>();
    }

    internal sealed class DownloadProgress
    {
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public int Percent { get; set; }
    }

    internal sealed class DownloadResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ZipPath { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
    }
}

