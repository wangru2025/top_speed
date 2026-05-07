using TopSpeed.Core.Updates;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class UpdateConfigBehaviorTests
{
    [Theory]
    [InlineData("android-arm64", "TopSpeed-android-arm64-Release-v-2026.4.18.apk")]
    [InlineData("android-arm", "TopSpeed-android-arm-Release-v-2026.4.18.apk")]
    [InlineData("mac-arm64", "TopSpeed-mac-arm64-Release-v-2026.4.18.zip")]
    [InlineData("windows-x64", "TopSpeed-windows-x64-Release-v-2026.4.18.zip")]
    [InlineData("linux-x64", "TopSpeed-linux-x64-Release-v-2026.4.18.zip")]
    public void BuildExpectedAssetName_ShouldSelectExpectedExtension(string runtimeTag, string expectedName)
    {
        var config = new UpdateConfig(
            "https://example.com/info.json",
            "https://example.com/latest",
            "TopSpeed-{runtime}-Release-v-{version}{ext}",
            runtimeTag,
            "Updater",
            "TopSpeed");

        var actual = config.BuildExpectedAssetName("2026.4.18");

        actual.Should().Be(expectedName);
    }

    [Fact]
    public void Default_ShouldPreferMirrorAndFallbackToForkGithubRelease()
    {
        var config = UpdateConfig.Default;

        config.Sources.Should().HaveCount(2);
        config.Sources[0].InfoUrl.Should().Be("https://ts.wangru.net/top_speed/info.json");
        config.Sources[0].LatestReleaseApiUrl.Should().Be("https://ts.wangru.net/top_speed/releases/latest");
        config.Sources[1].InfoUrl.Should().Be("https://raw.githubusercontent.com/wangru2025/top_speed/china-release/info.json");
        config.Sources[1].LatestReleaseApiUrl.Should().Be("https://api.github.com/repos/wangru2025/top_speed/releases/latest");
    }
}
