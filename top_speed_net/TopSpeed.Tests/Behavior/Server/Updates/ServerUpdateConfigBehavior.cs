using TopSpeed.Server.Updates;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class ServerUpdateConfigBehaviorTests
{
    [Fact]
    public void BuildExpectedAssetName_ShouldUseMacArm64Template()
    {
        var config = ServerUpdateConfig.Create("mac-arm64");

        var asset = config.BuildExpectedAssetName("2026.5.2");

        asset.Should().Be("TopSpeed.Server-mac-arm64-Release-v-2026.5.2.zip");
    }

    [Theory]
    [InlineData("mac-arm64", "mac-arm64")]
    [InlineData("MAC-ARM64", "mac-arm64")]
    [InlineData("mac-x64", "mac-x64")]
    [InlineData("auto", ServerUpdateConfig.AutoRuntimeAssetTag)]
    public void NormalizeConfiguredRuntimeAssetTag_ShouldAcceptKnownMacTargets(string rawValue, string expected)
    {
        var normalized = ServerUpdateConfig.NormalizeConfiguredRuntimeAssetTag(rawValue);

        normalized.Should().Be(expected);
    }
}
