using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.TeamEligibility;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.TeamEligibilityResultPanel")]
public sealed class TeamEligibilityResultPanelTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<TeamEligibilityResultPanel> Render(
        TeamEligibilityResultViewModel result, Action? onCheckAnother = null)
        => _ctx.Render<TeamEligibilityResultPanel>(p => p
            .Add(x => x.Result, result)
            .Add(x => x.OnCheckAnother, () => onCheckAnother?.Invoke()));

    [Fact(DisplayName = "Eligible should say the team can bowl together and show the pro-title note, with no contact line")]
    public void Render_ShouldShowEligibleState()
    {
        // Arrange & Act
        var cut = Render(new TeamEligibilityResultViewModel { Outcome = TeamEligibilityOutcomeKind.Eligible });

        // Assert
        cut.Markup.ShouldContain("This team can bowl together.");
        cut.Find(".result--ok").ShouldNotBeNull();
        cut.Find(".result__note").TextContent.ShouldContain("PBA or PWBA");
        cut.FindAll(".result__contact").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Ineligible should list each reason, show the contact line and no pro-title note")]
    public void Render_ShouldShowIneligibleReasonsAndContact()
    {
        // Arrange & Act
        var cut = Render(new TeamEligibilityResultViewModel
        {
            Outcome = TeamEligibilityOutcomeKind.Ineligible,
            Reasons = ["Too many champions.", "Not enough non-champions."]
        });

        // Assert
        cut.Markup.ShouldContain("This team can't bowl together.");
        cut.FindAll("li").Select(item => item.TextContent).ShouldBe(["Too many champions.", "Not enough non-champions."]);
        cut.Find(".result__contact a").GetAttribute("href").ShouldBe("mailto:info@bowlneba.com");
        cut.FindAll(".result__note").ShouldBeEmpty();
    }

    [Fact(DisplayName = "DependsOnDate should show a row per date with its verdict, the pro-title note and the contact line")]
    public void Render_ShouldShowDependsOnDateRows()
    {
        // Arrange & Act
        var cut = Render(new TeamEligibilityResultViewModel
        {
            Outcome = TeamEligibilityOutcomeKind.DependsOnDate,
            DateChecks =
            [
                new TeamEligibilityDateCheckViewModel(new DateOnly(2026, 3, 7), true, []),
                new TeamEligibilityDateCheckViewModel(new DateOnly(2026, 3, 8), false, ["Both in one group."])
            ]
        });

        // Assert
        cut.Markup.ShouldContain("The answer depends on the squad date.");
        var rows = cut.FindAll(".result__date");
        rows.Count.ShouldBe(2);
        rows[0].ClassList.ShouldContain("result__date--good");
        rows[0].TextContent.ShouldContain("Can bowl together");
        rows[1].ClassList.ShouldContain("result__date--bad");
        rows[1].TextContent.ShouldContain("Can't bowl together. Both in one group.");
        cut.FindAll(".result__note").Count.ShouldBe(1);
        cut.FindAll(".result__contact").Count.ShouldBe(1);
    }

    [Theory(DisplayName = "DefendingChampions should name the tournament won and why a truncated chain still counts")]
    [InlineData(false, false, "The champion limits do not apply.", TestDisplayName = "Normal")]
    [InlineData(true, false, "That tournament was truncated, and they still count as defending champions.", TestDisplayName = "Truncated source")]
    [InlineData(false, true, "A later tournament was truncated, so they never got to defend.", TestDisplayName = "Undefended")]
    public void Render_ShouldExplainDefendingChampions(bool truncated, bool undefended, string expectedPhrase)
    {
        // Arrange
        var result = new TeamEligibilityResultViewModel
        {
            Outcome = TeamEligibilityOutcomeKind.DefendingChampions,
            DefendingSource = new DefendingTeamViewModel("Joe's Doubles", "2025 Season", truncated, undefended, [])
        };

        // Act
        var cut = Render(result);

        // Assert
        var text = cut.Find(".result p").TextContent;
        text.ShouldContain("This team won Joe's Doubles (2025 Season).");
        text.ShouldContain(expectedPhrase);
        cut.Find(".result--defend").ShouldNotBeNull();
        cut.FindAll(".result__contact").ShouldBeEmpty();
    }

    [Fact(DisplayName = "DefendingChampions without a source should still say the limits do not apply")]
    public void Render_ShouldSayLimitsDoNotApply_WhenNoSource()
    {
        // Arrange & Act
        var cut = Render(new TeamEligibilityResultViewModel { Outcome = TeamEligibilityOutcomeKind.DefendingChampions });

        // Assert
        cut.Find(".result p").TextContent.ShouldBe("The champion limits do not apply.");
    }

    [Fact(DisplayName = "Should raise OnCheckAnother when 'Check another team' is clicked")]
    public async Task Click_ShouldRaiseOnCheckAnother()
    {
        // Arrange
        var raised = 0;
        var cut = Render(new TeamEligibilityResultViewModel { Outcome = TeamEligibilityOutcomeKind.Eligible }, () => raised++);

        // Act
        await cut.Find("button").ClickAsync();

        // Assert
        raised.ShouldBe(1);
    }
}
