using Neba.TestFactory.Attributes;
using Neba.Website.Server.Documents;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.TournamentRulesAnchors")]
public sealed class TournamentRulesAnchorsTests
{
    [Theory(DisplayName = "TryGet maps each type with its own rule section to its slug and rule number")]
    [InlineData("Singles", "singles", "rule-400-")]
    [InlineData("Doubles", "doubles", "rule-401-")]
    [InlineData("Trios", "trios", "rule-402-")]
    [InlineData("Baker", "baker", "rule-403-")]
    [InlineData("Tournament of Champions", "tournament-of-champions", "rule-600-")]
    [InlineData("Invitational", "invitational", "rule-601-")]
    [InlineData("Masters", "masters", "rule-602-")]
    [InlineData("Non-Champions", "non-champions", "rule-603-")]
    public void TryGet_ShouldReturnAnchor_WhenTypeHasRuleSection(string type, string slug, string prefix)
    {
        // Arrange & Act
        var found = TournamentRulesAnchors.TryGet(type, out var anchor);

        // Assert
        found.ShouldBeTrue();
        anchor!.Slug.ShouldBe(slug);
        anchor.Href.ShouldBe("/tournaments/rules#" + slug);
        anchor.Alias.HeadingIdPrefix.ShouldBe(prefix);
    }

    [Theory(DisplayName = "TryGet returns false for types without their own rule section")]
    [InlineData("Senior")]
    [InlineData("Women")]
    [InlineData("Youth")]
    [InlineData("Senior / Women")]
    [InlineData("Over/Under 50 Doubles")]
    [InlineData("High Roller")]
    public void TryGet_ShouldReturnFalse_WhenTypeHasNoRuleSection(string type)
    {
        // Arrange & Act
        var found = TournamentRulesAnchors.TryGet(type, out var anchor);

        // Assert
        found.ShouldBeFalse();
        anchor.ShouldBeNull();
    }

    [Fact(DisplayName = "AnchorAliases has one entry per slug, and every prefix ends in a hyphen")]
    public void AnchorAliases_ShouldHaveUniqueSlugsAndHyphenatedPrefixes()
    {
        // Arrange & Act
        var aliases = TournamentRulesAnchors.AnchorAliases;

        // Assert
        aliases.Count.ShouldBe(8);
        aliases.Values.ShouldAllBe(a => a.HeadingIdPrefix.EndsWith('-'));
        aliases["invitational"].TitleKeyword.ShouldBe("Invitational");
    }

    [Theory(DisplayName = "TryGetTeamRules maps each team format to its team composition and champion definition sub-rules")]
    [InlineData("Doubles", "401a,401b", "/tournaments/rules#doubles")]
    [InlineData("Over/Under 50 Doubles", "401a,401b", "/tournaments/rules#doubles")]
    [InlineData("Over/Under 40 Doubles", "401a,401b", "/tournaments/rules#doubles")]
    [InlineData("Trios", "402a,402b", "/tournaments/rules#trios")]
    [InlineData("Baker", "403a,402b", "/tournaments/rules#baker")]
    public void TryGetTeamRules_ShouldReturnSubRules_WhenTypeIsATeamFormat(string type, string expectedPrefixes, string expectedHref)
    {
        // Arrange & Act
        var found = TournamentRulesAnchors.TryGetTeamRules(type, out var anchor);

        // Assert
        found.ShouldBeTrue();
        string.Join(",", anchor!.SubRules.Select(subRule => subRule.HeadingPrefix)).ShouldBe(expectedPrefixes);
        anchor.FullRulesHref.ShouldBe(expectedHref);
    }

    [Theory(DisplayName = "TryGetTeamRules returns false for single-bowler formats")]
    [InlineData("Singles")]
    [InlineData("Invitational")]
    [InlineData("Non-Champions")]
    [InlineData("Unknown Format")]
    public void TryGetTeamRules_ShouldReturnFalse_WhenTypeIsNotATeamFormat(string type)
    {
        // Arrange & Act
        var found = TournamentRulesAnchors.TryGetTeamRules(type, out var anchor);

        // Assert
        found.ShouldBeFalse();
        anchor.ShouldBeNull();
    }

    [Fact(DisplayName = "TryGetTeamRules ignores the case of the tournament type")]
    public void TryGetTeamRules_ShouldIgnoreCase()
    {
        // Arrange & Act
        var found = TournamentRulesAnchors.TryGetTeamRules("trios", out _);

        // Assert
        found.ShouldBeTrue();
    }

    [Fact(DisplayName = "SubRuleRef builds its heading prefix from the rule number and letter")]
    public void SubRuleRef_ShouldBuildHeadingPrefix()
    {
        // Arrange & Act
        var subRule = new SubRuleRef(402, 'b', "Champion Definition");

        // Assert
        subRule.HeadingPrefix.ShouldBe("402b");
    }
}
