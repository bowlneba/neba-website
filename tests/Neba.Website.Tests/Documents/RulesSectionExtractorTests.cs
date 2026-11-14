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

    // Shape of the real document: lettered sub-rules are h4 headings with opaque ids, 402b has h5 children.
    private const string TeamRulesHtml =
        "<style>ol.lst-kix{list-style:none}</style>" +
        "<h3 id=\"h.gfg1eow43b1z\">Rule 401 - Doubles Tournament Format</h3>" +
        "<h4 id=\"h.i34k8d87vsx\">401a. Team Composition</h4><p>Entries are two-person teams. <a href=\"#h.ssua519ioj21\">see 402b</a></p>" +
        "<h4 id=\"h.6b0g84ucy713\">401b. Champion Definition</h4><p>A champion is a title holder.</p>" +
        "<h4 id=\"h.8mrk89jd6fnp\">401c. Re-Entries</h4><p>Re-entries are permitted.</p>" +
        "<h3 id=\"h.pe9rxguiua35\">Rule 402 - Trios Tournament Format</h3>" +
        "<h4 id=\"h.ssua519ioj21\">402b. Champion Definition</h4>" +
        "<h5 id=\"h.78xuauepv9sy\">One Time Champion</h5><p>Won a single title.</p>" +
        "<h4 id=\"h.axsjlqpio9vu\">402c. Qualifying Format</h4><p>Qualifying text.</p>";

    [Fact(DisplayName = "ExtractSubRule cuts a lettered sub-rule up to the next heading of the same level")]
    public void ExtractSubRule_ShouldStopAtNextSubRule_WhenSubRuleFound()
    {
        // Arrange
        var subRule = new SubRuleRef(401, 'a', "Team Composition");

        // Act
        var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, RulesPath, includeStyles: false);

        // Assert
        html.ShouldNotBeNull();
        html.ShouldContain("Entries are two-person teams.");
        html.ShouldNotContain("A champion is a title holder.");
    }

    [Fact(DisplayName = "ExtractSubRule keeps the h5 children of a sub-rule")]
    public void ExtractSubRule_ShouldKeepChildHeadings_WhenSubRuleHasThem()
    {
        // Arrange
        var subRule = new SubRuleRef(402, 'b', "Champion Definition");

        // Act
        var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, RulesPath, includeStyles: false);

        // Assert
        html.ShouldNotBeNull();
        html.ShouldContain("Won a single title.");
        html.ShouldNotContain("Qualifying text.");
    }

    [Fact(DisplayName = "ExtractSubRule returns null when no heading matches")]
    public void ExtractSubRule_ShouldReturnNull_WhenSubRuleMissing()
    {
        // Arrange
        var subRule = new SubRuleRef(405, 'a', "Team Composition");

        // Act
        var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, RulesPath, includeStyles: false);

        // Assert
        html.ShouldBeNull();
    }

    [Fact(DisplayName = "ExtractSubRule needs the keyword as well as the number and letter")]
    public void ExtractSubRule_ShouldReturnNull_WhenKeywordDoesNotMatch()
    {
        // Arrange
        var subRule = new SubRuleRef(401, 'a', "Qualifying");

        // Act
        var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, RulesPath, includeStyles: false);

        // Assert
        html.ShouldBeNull();
    }

    [Fact(DisplayName = "ExtractSubRule matches a heading that starts with Rule")]
    public void ExtractSubRule_ShouldMatchHeadingWithRulePrefix()
    {
        // Arrange
        const string doc = "<h4 id=\"x\">Rule 403a. Team Composition Criteria</h4><p>Baker text.</p><h4 id=\"y\">403b. Other</h4><p>Other text.</p>";
        var subRule = new SubRuleRef(403, 'a', "Team Composition");

        // Act
        var html = RulesSectionExtractor.ExtractSubRule(doc, subRule, RulesPath, includeStyles: false);

        // Assert
        html.ShouldNotBeNull().ShouldContain("Baker text.");
    }

    [Theory(DisplayName = "ExtractSubRule carries the list styles only when asked")]
    [InlineData(true, true, TestDisplayName = "Styles included")]
    [InlineData(false, false, TestDisplayName = "Styles left out")]
    public void ExtractSubRule_ShouldHonorIncludeStyles(bool includeStyles, bool expectStyle)
    {
        // Arrange
        var subRule = new SubRuleRef(401, 'a', "Team Composition");

        // Act
        var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, RulesPath, includeStyles);

        // Assert
        html.ShouldNotBeNull().Contains("<style>", StringComparison.Ordinal).ShouldBe(expectStyle);
    }

    [Fact(DisplayName = "ExtractSubRule points same-document links at the rules page")]
    public void ExtractSubRule_ShouldRewriteSameDocumentLinks()
    {
        // Arrange
        var subRule = new SubRuleRef(401, 'a', "Team Composition");

        // Act
        var html = RulesSectionExtractor.ExtractSubRule(TeamRulesHtml, subRule, RulesPath, includeStyles: false);

        // Assert
        html.ShouldNotBeNull().ShouldContain("href=\"/tournaments/rules#h.ssua519ioj21\"");
    }
}