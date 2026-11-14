using Bunit;

using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Documents;
using Neba.Website.Server.Tournaments.TeamEligibility;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.TeamRules")]
public sealed class TeamRulesTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Mock<IRulesSectionService> _rules = new(MockBehavior.Strict);

    public TeamRulesTests()
    {
        _ctx.Services.AddSingleton(_rules.Object);
        _ctx.Services.AddSingleton<ILogger<TeamRules>>(NullLogger<TeamRules>.Instance);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render the team rules HTML under a heading for a team format")]
    public void Render_ShouldShowRules_ForTeamFormat()
    {
        // Arrange
        _rules
            .Setup(x => x.GetTeamRulesAsync(It.IsAny<TeamRulesAnchor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RulesSection("<h4>401a. Team Composition</h4><p>Two bowlers.</p>", null));

        // Act
        var cut = _ctx.Render<TeamRules>(p => p.Add(x => x.TournamentType, "Doubles"));

        // Assert
        cut.Find("#team-rules-heading").TextContent.ShouldBe("Team rules");
        cut.Find(".team-rules__body").InnerHtml.ShouldContain("401a. Team Composition");
        _rules.Verify(x => x.GetTeamRulesAsync(It.Is<TeamRulesAnchor>(a => a.SubRules[0].HeadingPrefix == "401a"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Should render nothing for a format without team rules")]
    public void Render_ShouldRenderNothing_ForSingles()
    {
        // Arrange & Act
        var cut = _ctx.Render<TeamRules>(p => p.Add(x => x.TournamentType, "Singles"));

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Theory(DisplayName = "Should offer the full rules link when the rules cannot be loaded")]
    [InlineData(true, TestDisplayName = "Section missing")]
    [InlineData(false, TestDisplayName = "Request failed")]
    public void Render_ShouldLinkToFullRules_WhenRulesFailToLoad(bool notFound)
    {
        // Arrange
        Error error = notFound ? Error.NotFound("RulesSection.NotFound", "missing") : Error.Failure("RulesSection.LoadFailed", "failed");
        _rules
            .Setup(x => x.GetTeamRulesAsync(It.IsAny<TeamRulesAnchor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(error);

        // Act
        var cut = _ctx.Render<TeamRules>(p => p.Add(x => x.TournamentType, "Trios"));

        // Assert
        cut.Find(".team-rules__status a").GetAttribute("href").ShouldBe("/tournaments/rules#trios");
    }

    [Fact(DisplayName = "Should offer the full rules link when loading the rules throws")]
    public void Render_ShouldLinkToFullRules_WhenLoadingThrows()
    {
        // Arrange
        _rules
            .Setup(x => x.GetTeamRulesAsync(It.IsAny<TeamRulesAnchor>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var cut = _ctx.Render<TeamRules>(p => p.Add(x => x.TournamentType, "Baker"));

        // Assert
        cut.Find(".team-rules__status a").GetAttribute("href").ShouldBe("/tournaments/rules#baker");
    }

    [Fact(DisplayName = "Should load the rules again only when the tournament type changes")]
    public void Parameters_ShouldReloadOnlyWhenTypeChanges()
    {
        // Arrange
        _rules
            .Setup(x => x.GetTeamRulesAsync(It.IsAny<TeamRulesAnchor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RulesSection("<p>Rules</p>", null));
        var cut = _ctx.Render<TeamRules>(p => p.Add(x => x.TournamentType, "Doubles"));

        // Act
        cut.Render(p => p.Add(x => x.TournamentType, "Doubles"));
        cut.Render(p => p.Add(x => x.TournamentType, "Trios"));

        // Assert
        _rules.Verify(x => x.GetTeamRulesAsync(It.IsAny<TeamRulesAnchor>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}