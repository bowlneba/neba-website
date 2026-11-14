using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Tournaments.TeamEligibility;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.TeamEligibilityFacts")]
public sealed class TeamEligibilityFactsTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    // Collapses the markup's line breaks and indentation, so text reads as a person sees it.
    private static string Text(AngleSharp.Dom.IElement element)
        => string.Join(' ', element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static TeamEligibilityMemberViewModel Member(
        string name = "Chris Forry",
        ChampionStandingKind standing = ChampionStandingKind.NonChampion,
        int titles5 = 0,
        int? titles10 = null,
        int? lastTitle = null,
        bool? age = null,
        bool? hallOfFame = null,
        bool titlesBreak = false,
        bool ageBreaks = false,
        bool hallOfFameBreaks = false,
        string? usbcId = "4822-11840")
        => new(name, usbcId, false, lastTitle, titles5, titles10, standing, age, hallOfFame, titlesBreak, ageBreaks, hallOfFameBreaks);

    private static TeamEligibilityMemberViewModel NotInSystem()
        => new(null, null, true, null, 0, null, ChampionStandingKind.NonChampion, null, null, false, false, false);

    private IRenderedComponent<TeamEligibilityFacts> Render(
        IReadOnlyList<TeamEligibilityMemberViewModel> members,
        IReadOnlyList<TeamEligibilityCheckViewModel>? checks = null,
        string summary = "")
        => _ctx.Render<TeamEligibilityFacts>(p => p
            .Add(x => x.Members, members)
            .Add(x => x.Checks, checks ?? [])
            .Add(x => x.Summary, summary));

    [Fact(DisplayName = "Should render nothing when there are no members")]
    public void Render_ShouldRenderNothing_WhenNoMembers()
    {
        // Arrange & Act
        var cut = Render([]);

        // Assert
        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should put the standing on the top line with the name, and the last title and USBC ID under the name")]
    public void Render_ShouldShowStandingOnTopLine()
    {
        // Arrange & Act
        var cut = Render([Member(standing: ChampionStandingKind.MultiTimeChampion, titles5: 3, lastTitle: 2025)], summary: "Window summary.");

        // Assert
        var top = cut.Find(".facts__top");
        top.QuerySelector(".facts__name")!.TextContent.ShouldContain("Chris Forry");
        top.QuerySelector(".facts__name small")!.TextContent.ShouldBe("USBC 4822-11840 · Last NEBA title: 2025");
        top.QuerySelector(".facts__pill")!.TextContent.ShouldBe("Multi-time champion");
        cut.Find(".facts__summary").TextContent.ShouldBe("Window summary.");
    }

    [Theory(DisplayName = "Should label each standing")]
    [InlineData(ChampionStandingKind.NonChampion, "Non-champion", "non", TestDisplayName = "Non-champion")]
    [InlineData(ChampionStandingKind.OneTimeChampion, "One-time champion", "one", TestDisplayName = "One-time")]
    [InlineData(ChampionStandingKind.MultiTimeChampion, "Multi-time champion", "multi", TestDisplayName = "Multi-time")]
    public void Render_ShouldLabelStanding(ChampionStandingKind standing, string label, string cssSuffix)
    {
        // Arrange & Act
        var cut = Render([Member(standing: standing)]);

        // Assert
        var pill = cut.Find(".facts__pill");
        pill.TextContent.ShouldBe(label);
        pill.ClassList.ShouldContain("facts__pill--" + cssSuffix);
    }

    [Fact(DisplayName = "Should say there are no NEBA titles when the bowler has never won one")]
    public void Render_ShouldSayNoTitles_WhenNoLastTitle()
    {
        // Arrange & Act
        var cut = Render([Member(lastTitle: null, usbcId: null)]);

        // Assert
        cut.Find(".facts__name small").TextContent.ShouldBe("No NEBA titles");
    }

    [Fact(DisplayName = "Should show the 10-year card only when the format uses it, and mark the title cards when titles break a rule")]
    public void Render_ShouldShowTitleCards()
    {
        // Arrange & Act
        var doubles = Render([Member(titles5: 1, titles10: 2, titlesBreak: true)]);
        var trios = Render([Member(titles5: 1)]);

        // Assert
        doubles.FindAll(".facts__card").Select(Text).ShouldBe(["NEBA titles, last 5 years 1", "NEBA titles, last 10 years 2"]);
        doubles.FindAll(".facts__card--hit").Count.ShouldBe(2);
        trios.FindAll(".facts__card").Count.ShouldBe(1);
        trios.FindAll(".facts__card--hit").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show the age card with Yes or No, marked only when the missing age breaks the rule")]
    public void Render_ShouldShowAgeCard()
    {
        // Arrange & Act
        var cut = Render([Member(age: true), Member(name: "Nina Newcomer", age: false, ageBreaks: true)]);

        // Assert
        var ageCards = cut.FindAll(".facts__card").Where(card => card.TextContent.Contains("Age we can check", StringComparison.Ordinal)).ToList();
        ageCards[0].TextContent.ShouldContain("Yes");
        ageCards[0].ClassList.ShouldNotContain("facts__card--hit");
        ageCards[1].TextContent.ShouldContain("No");
        ageCards[1].ClassList.ShouldContain("facts__card--hit");
    }

    [Fact(DisplayName = "Should show the Hall of Fame card as Member or No, marked when the limit is broken")]
    public void Render_ShouldShowHallOfFameCard()
    {
        // Arrange & Act
        var cut = Render([Member(hallOfFame: true, hallOfFameBreaks: true), Member(hallOfFame: false)]);

        // Assert
        var cards = cut.FindAll(".facts__card").Where(card => card.TextContent.Contains("Hall of Fame", StringComparison.Ordinal)).ToList();
        cards[0].TextContent.ShouldContain("Member");
        cards[0].ClassList.ShouldContain("facts__card--hit");
        cards[1].TextContent.ShouldContain("No");
        cards[1].ClassList.ShouldNotContain("facts__card--hit");
    }

    [Fact(DisplayName = "Should show a member not in the system as counted as a non-champion, with no other cards")]
    public void Render_ShouldShowNotInSystemMember()
    {
        // Arrange & Act
        var cut = Render([NotInSystem()]);

        // Assert
        cut.Find(".facts__name").TextContent.ShouldContain("Not in our system");
        cut.Find(".facts__pill").TextContent.ShouldBe("Counted as a non-champion");
        cut.Find(".facts__pill").ClassList.ShouldContain("facts__pill--unknown");
        cut.FindAll(".facts__card").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should list each rule's tally as passed or failed, readable without the icon")]
    public void Render_ShouldListChecks()
    {
        // Arrange
        var checks = new[]
        {
            new TeamEligibilityCheckViewModel("Non-champions: 1 (needs at least 1)", true),
            new TeamEligibilityCheckViewModel("Multi-time champions: 2 (at most 1)", false)
        };

        // Act
        var cut = Render([Member()], checks);

        // Assert
        var items = cut.FindAll(".facts__check");
        items[0].ClassList.ShouldContain("facts__check--pass");
        Text(items[0]).ShouldContain("Passed: Non-champions: 1 (needs at least 1)");
        items[1].ClassList.ShouldContain("facts__check--fail");
        Text(items[1]).ShouldContain("Failed: Multi-time champions: 2 (at most 1)");
    }
}