using System.Linq;
using TopSpeed.Game;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class HelpDocumentResolverBehaviorTests
{
    [Fact]
    public void BuildCandidateRelativePaths_ShouldPreferSpecificLanguageThenParentThenEnglishThenRoot()
    {
        var candidates = HelpDocumentResolver.BuildCandidateRelativePaths("game-guide.html", "zh-CN");

        candidates.Should().Equal(
            "zh-CN/game-guide.html",
            "zh/game-guide.html",
            "en/game-guide.html",
            "game-guide.html");
    }

    [Fact]
    public void BuildCandidateRelativePaths_ShouldFallbackToEnglishThenRootWhenLanguageMissing()
    {
        var candidates = HelpDocumentResolver.BuildCandidateRelativePaths("game-guide.html", null);

        candidates.Should().Equal(
            "en/game-guide.html",
            "game-guide.html");
    }

    [Fact]
    public void BuildCandidateRelativePaths_ShouldAvoidDuplicateParentFallback()
    {
        var candidates = HelpDocumentResolver.BuildCandidateRelativePaths("game-guide.html", "zh");

        candidates.Count.Should().Be(3);
        candidates[0].Should().Be("zh/game-guide.html");
        candidates[1].Should().Be("en/game-guide.html");
        candidates.Last().Should().Be("game-guide.html");
    }

    [Fact]
    public void BuildCandidateRelativePaths_ShouldCanonicalizeLocaleFolderCasing()
    {
        var candidates = HelpDocumentResolver.BuildCandidateRelativePaths("game-guide.html", "ZH_cn");

        candidates[0].Should().Be("zh-CN/game-guide.html");
    }
}
