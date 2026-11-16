using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.DefendingChampions")]
public sealed class DefendingChampionsTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<DefendingChampions> Render(
        IReadOnlyList<DefendingTeamViewModel> teams, DefendingChampionsLayout? layout = null)
        => _ctx.Render<DefendingChampions>(p =>
        {
            p.Add(x => x.Teams, teams);
            if (layout is { } value)
            {
                p.Add(x => x.Layout, value);
            }
        });

    [Theory(DisplayName = "Should render nothing when there are no defending teams")]
    [InlineData(DefendingChampionsLayout.Strip, TestDisplayName = "Strip")]
    [InlineData(DefendingChampionsLayout.Panel, TestDisplayName = "Panel")]
    public void Render_ShouldRenderNothing_WhenThereAreNoTeams(DefendingChampionsLayout layout)
    {
        // Arrange & Act
        var cut = Render([], layout);

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Strip should show each champion in a pill and tag the winning season")]
    public void Render_ShouldShowPillsAndSeasonTag_InStripLayout()
    {
        // Arrange
        var team = DefendingTeamViewModelFactory.Create(champions: ["Dave Smith", "Dana Jones"]);

        // Act
        var cut = Render([team], DefendingChampionsLayout.Strip);

        // Assert
        cut.FindAll(".defending__pill").Select(pill => pill.TextContent).ShouldBe(["Dave Smith", "Dana Jones"]);
        cut.Find(".defending__tag").TextContent.Trim().ShouldBe("2025 Season winners");
        cut.FindAll(".defending--panel").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Strip should tag a single champion as winner, not winners")]
    public void Render_ShouldUseSingularWinner_WhenTeamHasOneChampion()
    {
        // Arrange
        var team = DefendingTeamViewModelFactory.Create();

        // Act
        var cut = Render([team], DefendingChampionsLayout.Strip);

        // Assert
        cut.Find(".defending__tag").TextContent.Trim().ShouldBe("2025 Season winner");
    }

    [Fact(DisplayName = "Strip should note a truncated tournament after the season")]
    public void Render_ShouldNoteTruncatedSource_InStripLayout()
    {
        // Arrange
        var team = DefendingTeamViewModelFactory.Create(sourceTruncated: true, champions: ["Dave Smith", "Dana Jones"]);

        // Act
        var cut = Render([team], DefendingChampionsLayout.Strip);

        // Assert
        cut.Find(".defending__tag").TextContent.Trim().ShouldBe("2025 Season winners, tournament truncated");
    }

    [Fact(DisplayName = "Strip should keep two teams apart after a truncation")]
    public void Render_ShouldGroupTeamsSeparately_WhenTwoTeams()
    {
        // Arrange
        var truncated = DefendingTeamViewModelFactory.Create(seasonDescription: "2026 Season", sourceTruncated: true, champions: ["A One", "B Two"]);
        var undefended = DefendingTeamViewModelFactory.Create(seasonDescription: "2025 Season", undefended: true, champions: ["C Three", "D Four"]);

        // Act
        var cut = Render([truncated, undefended], DefendingChampionsLayout.Strip);

        // Assert
        var groups = cut.FindAll(".defending__group");
        groups.Count.ShouldBe(2);
        groups[0].QuerySelectorAll(".defending__pill").Select(pill => pill.TextContent).ShouldBe(["A One", "B Two"]);
        groups[1].QuerySelector(".defending__tag")!.TextContent.Trim().ShouldBe("2025 Season winners, could not defend");
    }

    [Fact(DisplayName = "Panel should list each team on its own row with its names joined")]
    public void Render_ShouldListRowPerTeam_InPanelLayout()
    {
        // Arrange
        var truncated = DefendingTeamViewModelFactory.Create(seasonDescription: "2026 Season", sourceTruncated: true, champions: ["A One", "B Two"]);
        var undefended = DefendingTeamViewModelFactory.Create(seasonDescription: "2025 Season", undefended: true, champions: ["C Three", "D Four"]);

        // Act
        var cut = Render([truncated, undefended], DefendingChampionsLayout.Panel);

        // Assert
        var rows = cut.FindAll(".defending__row");
        rows.Count.ShouldBe(2);
        rows[0].QuerySelector(".defending__names")!.TextContent.ShouldBe("A One / B Two");
        rows[0].QuerySelector(".defending__season small")!.TextContent.ShouldBe("tournament truncated");
        rows[1].QuerySelector(".defending__season small")!.TextContent.ShouldBe("could not defend");
        cut.FindAll(".defending--strip").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Panel should not add a note line for a normal defending team")]
    public void Render_ShouldNotAddNote_ForNormalTeam()
    {
        // Arrange & Act
        var cut = Render([DefendingTeamViewModelFactory.Create()], DefendingChampionsLayout.Panel);

        // Assert
        cut.FindAll(".defending__season small").ShouldBeEmpty();
    }

    [Theory(DisplayName = "Heading should be plural for a team of more than one bowler or for two teams")]
    [InlineData(2, 1, "Defending champions", TestDisplayName = "A two-bowler team")]
    [InlineData(1, 1, "Defending champion", TestDisplayName = "One bowler, one team")]
    [InlineData(1, 2, "Defending champions", TestDisplayName = "One bowler, two teams")]
    public void Render_ShouldPluralizeHeading(int bowlers, int teams, string expected)
    {
        // Arrange
        var names = Enumerable.Range(1, bowlers).Select(index => "Bowler " + index).ToArray();
        var list = Enumerable.Range(0, teams).Select(_ => DefendingTeamViewModelFactory.Create(champions: names)).ToArray();

        // Act
        var cut = Render(list, DefendingChampionsLayout.Strip);

        // Assert
        cut.Find(".defending__label").TextContent.Trim().ShouldEndWith(expected);
    }
}