using System.Linq;
using TopSpeed.Game;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class HelpDocumentResolverBehaviorTests
{
    [Fact]
    public void BuildCandidateFileNames_ShouldPreferSpecificLanguageThenParentThenEnglish()
    {
        var candidates = HelpDocumentResolver.BuildCandidateFileNames("game-guide.html", "zh-CN");

        candidates.Should().Equal(
            "game-guide.zh-cn.html",
            "game-guide.zh.html",
            "game-guide.html");
    }

    [Fact]
    public void BuildCandidateFileNames_ShouldFallbackToEnglishWhenLanguageMissing()
    {
        var candidates = HelpDocumentResolver.BuildCandidateFileNames("game-guide.html", null);

        candidates.Should().Equal("game-guide.html");
    }

    [Fact]
    public void BuildCandidateFileNames_ShouldAvoidDuplicateParentFallback()
    {
        var candidates = HelpDocumentResolver.BuildCandidateFileNames("game-guide.html", "zh");

        candidates.Count.Should().Be(2);
        candidates[0].Should().Be("game-guide.zh.html");
        candidates.Last().Should().Be("game-guide.html");
    }
}
