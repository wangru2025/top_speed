using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class LocalizationTemplateAuditBehaviorTests
{
    private static readonly Regex PlaceholderOnlyMsgId = new(
        "msgid \"\\{[0-9]+\\}( \\{[0-9]+\\})*\"",
        RegexOptions.Compiled);

    private static readonly Regex PlaceholderOnlyMark = new(
        "LocalizationService\\.Mark\\(\"\\{[0-9]+\\}( \\{[0-9]+\\})*\"\\)",
        RegexOptions.Compiled);

    [Fact]
    public void Templates_ShouldNotContainPlaceholderOnlyGarbageStrings()
    {
        var root = FindTopSpeedNetRoot();
        var files = Directory.GetFiles(Path.Combine(root, "languages"), "messages.pot", SearchOption.AllDirectories);

        files.Should().NotBeEmpty();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            PlaceholderOnlyMsgId.IsMatch(text).Should().BeFalse(file);
        }
    }

    [Fact]
    public void Source_ShouldNotMarkPlaceholderOnlyGarbageStrings()
    {
        var root = FindTopSpeedNetRoot();
        var sourceRoots = new[]
        {
            Path.Combine(root, "TopSpeed"),
            Path.Combine(root, "TopSpeed.Server"),
            Path.Combine(root, "TopSpeed.Shared")
        };

        foreach (var sourceRoot in sourceRoots)
        {
            foreach (var file in Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                PlaceholderOnlyMark.IsMatch(text).Should().BeFalse(file);
            }
        }
    }

    private static string FindTopSpeedNetRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "top_speed_net");
            if (Directory.Exists(candidate))
                return candidate;

            if (string.Equals(directory.Name, "top_speed_net", StringComparison.OrdinalIgnoreCase))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate top_speed_net from test output directory.");
    }
}
