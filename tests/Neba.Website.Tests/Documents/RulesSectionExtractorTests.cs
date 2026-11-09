using Neba.TestFactory.Attributes;
using Neba.Website.Server.Documents;

namespace Neba.Website.Tests.Documents;

[UnitTest]
[Component("Website.Documents.RulesSectionExtractor")]
public sealed class RulesSectionExtractorTests
{
    private const string RulesPath = "/tournaments/rules";

    private static readonly AnchorAlias Invitational = new("rule-601-", "Invitational");

    private const string Doc =
        "<style>ol.lst-kix{list-style:none}</style>" +
        "<h3 id=\"rule-600-tournament-of-champions\">Rule 600 - Tournament of Champions</h3><p>TOC text</p>" +
        "<h3 id=\"rule-601-invitational-tournament-rules\">Rule 601- Invitational Tournament Rules</h3>" +
        "<p>Eligibility <a href=\"#rule-602-masters-tournament-rules\">see 602</a></p>" +
        "<h4 id=\"rule-601a.-general-provisions\">Rule 601a. General Provisions</h4><p>Sub text</p>" +
        "<h3 id=\"rule-602-masters-tournament-rules\">Rule 602 - Masters Tournament Rules</h3><p>Masters text</p>";

    [Fact(DisplayName = "Returns the heading and its content up to the next heading of the same or higher level")]
    public void Extract_ShouldReturnSectionIncludingSubHeadings()
    {
        // Arrange & Act
        var html = RulesSectionExtractor.Extract(Doc, Invitational, RulesPath);

        // Assert
        html.ShouldNotBeNull();
        html.ShouldContain("Rule 601- Invitational");
        html.ShouldContain("Eligibility");
        html.ShouldContain("Rule 601a. General Provisions");
        html.ShouldContain("Sub text");
        html.ShouldNotContain("Masters text");
        html.ShouldNotContain("TOC text");
    }

    [Fact(DisplayName = "Copies style blocks")]
    public void Extract_ShouldCopyStyleBlocks()
    {
        // Arrange & Act
        var html = RulesSectionExtractor.Extract(Doc, Invitational, RulesPath);

        // Assert
        html.ShouldNotBeNull().ShouldContain("<style>ol.lst-kix{list-style:none}</style>");
    }

    [Fact(DisplayName = "Rewrites same-document links to the rules page")]
    public void Extract_ShouldRewriteHashLinks()
    {
        // Arrange & Act
        var html = RulesSectionExtractor.Extract(Doc, Invitational, RulesPath);

        // Assert
        html.ShouldNotBeNull().ShouldContain("href=\"/tournaments/rules#rule-602-masters-tournament-rules\"");
    }

    [Fact(DisplayName = "Falls back to the keyword when the rule was renumbered")]
    public void Extract_ShouldFallBackToKeyword_WhenRenumbered()
    {
        // Arrange
        const string renumbered = "<h3 id=\"rule-701-invitational-tournament-rules\">Rule 701- Invitational</h3><p>Moved</p>";

        // Act
        var html = RulesSectionExtractor.Extract(renumbered, Invitational, RulesPath);

        // Assert
        html.ShouldNotBeNull().ShouldContain("Moved");
    }

    [Fact(DisplayName = "Does not match rule-60- for a rule-601- prefix")]
    public void Extract_ShouldNotMatchShorterRuleNumber()
    {
        // Arrange
        const string doc = "<h3 id=\"rule-60-masters\">Rule 60- Masters</h3><p>x</p>";

        // Act
        var html = RulesSectionExtractor.Extract(doc, Invitational, RulesPath);

        // Assert
        html.ShouldBeNull();
    }

    [Fact(DisplayName = "Never chooses a lettered sub-rule")]
    public void Extract_ShouldIgnoreLetteredSubRules()
    {
        // Arrange
        const string doc = "<h3 id=\"rule-703e-invitational\">Rule 703e. Invitational</h3><p>x</p>";

        // Act
        var html = RulesSectionExtractor.Extract(doc, Invitational, RulesPath);

        // Assert
        html.ShouldBeNull();
    }

    [Fact(DisplayName = "Returns null when nothing matches")]
    public void Extract_ShouldReturnNull_WhenNoHeadingMatches()
    {
        // Arrange & Act
        var html = RulesSectionExtractor.Extract("<h3 id=\"rule-100-general\">Rule 100</h3>", Invitational, RulesPath);

        // Assert
        html.ShouldBeNull();
    }

    [Fact(DisplayName = "Returns a section larger than the 32 KB SignalR message limit")]
    public void Extract_ShouldHandleLargeSections()
    {
        // Arrange
        var body = string.Concat(Enumerable.Repeat("<p>Rule text that fills the section.</p>", 2000));
        var doc = "<h3 id=\"rule-601-invitational-tournament-rules\">Rule 601- Invitational</h3>" + body +
                  "<h3 id=\"rule-602-masters-tournament-rules\">Rule 602 - Masters</h3>";

        // Act
        var html = RulesSectionExtractor.Extract(doc, Invitational, RulesPath);

        // Assert
        html.ShouldNotBeNull().Length.ShouldBeGreaterThan(32 * 1024);
    }
}
