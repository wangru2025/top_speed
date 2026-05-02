using System.Runtime.InteropServices;
using TopSpeed.Runtime;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class RuntimeAssetResolverBehaviorTests
{
    [Theory]
    [InlineData("win-x64", Architecture.X64, false, false, false, "windows-x64")]
    [InlineData("android-arm64", Architecture.Arm64, false, false, false, "android-arm64")]
    [InlineData("android-arm", Architecture.Arm, false, false, false, "android-arm")]
    [InlineData("linux-x64", Architecture.X64, false, false, false, "linux-x64")]
    [InlineData("osx-x64", Architecture.X64, false, false, false, "mac-x64")]
    [InlineData("osx-arm64", Architecture.Arm64, false, false, false, "mac-arm64")]
    [InlineData("osx.13-arm64", Architecture.Arm64, false, false, false, "mac-arm64")]
    [InlineData("", Architecture.X64, true, false, false, "windows-x64")]
    [InlineData("", Architecture.X64, false, true, false, "linux-x64")]
    [InlineData("", Architecture.X64, false, false, true, "mac-x64")]
    [InlineData("", Architecture.Arm64, false, false, true, "mac-arm64")]
    public void DetectClientRuntimeAssetTag_ShouldMatchReleaseTemplate(
        string runtimeIdentifier,
        Architecture architecture,
        bool isWindows,
        bool isLinux,
        bool isMacOs,
        string expected)
    {
        var actual = RuntimeAssetResolver.DetectClientRuntimeAssetTag(
            runtimeIdentifier,
            architecture,
            isWindows,
            isLinux,
            isMacOs);

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("win-x64", Architecture.X64, false, false, false, "win-x64")]
    [InlineData("linux-x64", Architecture.X64, false, false, false, "linux-x64")]
    [InlineData("linux-arm64", Architecture.X64, false, false, false, "linux-arm64")]
    [InlineData("linux-arm", Architecture.X64, false, false, false, "linux-arm32")]
    [InlineData("linux-musl-x64", Architecture.X64, false, false, false, "linux-musl-x64")]
    [InlineData("linux-musl-arm64", Architecture.X64, false, false, false, "linux-musl-arm64")]
    [InlineData("osx-x64", Architecture.X64, false, false, false, "mac-x64")]
    [InlineData("osx-arm64", Architecture.Arm64, false, false, false, "mac-arm64")]
    [InlineData("osx.13-arm64", Architecture.Arm64, false, false, false, "mac-arm64")]
    [InlineData("", Architecture.X64, true, false, false, "win-x64")]
    [InlineData("", Architecture.X64, false, true, false, "linux-x64")]
    [InlineData("", Architecture.X64, false, false, true, "mac-x64")]
    [InlineData("", Architecture.Arm64, false, false, true, "mac-arm64")]
    [InlineData("", Architecture.Arm64, false, true, false, "linux-arm64")]
    [InlineData("", Architecture.Arm, false, true, false, "linux-arm32")]
    [InlineData("", Architecture.X86, false, true, false, "linux-x86-fdd")]
    public void DetectServerRuntimeAssetTag_ShouldUseHardenedRidChecks(
        string runtimeIdentifier,
        Architecture architecture,
        bool isWindows,
        bool isLinux,
        bool isMacOs,
        string expected)
    {
        var actual = RuntimeAssetResolver.DetectServerRuntimeAssetTag(
            runtimeIdentifier,
            architecture,
            isWindows,
            isLinux,
            isMacOs);

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("Updater", true, "Updater.exe")]
    [InlineData("Updater", false, "Updater")]
    [InlineData("TopSpeed.Server", true, "TopSpeed.Server.exe")]
    [InlineData("TopSpeed.Server", false, "TopSpeed.Server")]
    public void ResolveExecutableFileName_ShouldNotRequireCallersToPassExtensions(string stem, bool isWindows, string expected)
    {
        var actual = RuntimeAssetResolver.ResolveExecutableFileName(stem, isWindows);

        actual.Should().Be(expected);
    }
}
