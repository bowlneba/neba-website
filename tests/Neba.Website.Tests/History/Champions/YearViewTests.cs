using System.Globalization;

using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Champions;
using Neba.Website.Server.History.Champions;

namespace Neba.Website.Tests.History.Champions;

[UnitTest]
[Component("Website.History.Champions.YearView")]
public sealed class YearViewTests : IDisposable
{
    private readonly BunitContext _ctx;

    public YearViewTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    // ── Render ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render a section for each year in descending order")]
    public void Render_ShouldRenderYearSectionForEachYear()
    {
        // Arrange
        var year1 = TitlesByYearViewModelFactory.Create(year: 2024, titles: [BowlerTitleViewModelFactory.Create()]);
        var year2 = TitlesByYearViewModelFactory.Create(year: 2023, titles: [BowlerTitleViewModelFactory.Create()]);

        // Act
        var cut = Render([year1, year2], []);

        // Assert
        cut.FindAll(".year-section").Count.ShouldBe(2);
        cut.Markup.ShouldContain("2024");
        cut.Markup.ShouldContain("2023");
    }

    [Fact(DisplayName = "Should show month abbreviation for each tournament row")]
    public void Render_ShouldShowMonthAbbreviation_ForEachTournamentRow()
    {
        // Arrange
        var title = BowlerTitleViewModelFactory.Create(tournamentMonth: 6);
        var year = TitlesByYearViewModelFactory.Create(year: 2024, titles: [title]);

        // Act
        var cut = Render([year], []);

        // Assert
        cut.Markup.ShouldContain("Jun");
    }

    [Fact(DisplayName = "Should show bowler name as a link in the champions column")]
    public void Render_ShouldShowBowlerNameAsLink()
    {
        // Arrange
        var title = BowlerTitleViewModelFactory.Create(bowlerName: "Jane Smith");
        var year = TitlesByYearViewModelFactory.Create(year: 2024, titles: [title]);

        // Act
        var cut = Render([year], []);

        // Assert
        var links = cut.FindAll("a.champ-link");
        links.ShouldNotBeEmpty();
        links[0].TextContent.Trim().ShouldBe("Jane Smith");
    }

    [Fact(DisplayName = "Should show separator between multiple champions in the same tournament")]
    public void Render_ShouldShowSeparator_BetweenMultipleChampions()
    {
        // Arrange — two bowlers sharing the same tournament ID so they appear in one row
        const string tournamentId = "01000000000000000000000001";
        var title1 = BowlerTitleViewModelFactory.Create(bowlerName: "Alice") with
        {
            TournamentId = tournamentId,
            TournamentMonth = 4,
            TournamentYear = 2024,
            TournamentType = "Singles",
        };
        var title2 = BowlerTitleViewModelFactory.Create(bowlerName: "Bob") with
        {
            TournamentId = tournamentId,
            TournamentMonth = 4,
            TournamentYear = 2024,
            TournamentType = "Singles",
        };
        var year = TitlesByYearViewModelFactory.Create(year: 2024, titles: [title1, title2]);

        // Act
        var cut = Render([year], []);

        // Assert
        cut.Markup.ShouldContain("champ-sep");
    }

    // ── OnParametersSet — collapse initialization ─────────────────────────

    [Fact(DisplayName = "Should expand the 3 most recent years on initial load")]
    public void OnParametersSet_ShouldExpandThreeMostRecentYears()
    {
        // Arrange — 5 years; top 3 should be expanded
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2023),
            TitlesByYearViewModelFactory.Create(year: 2022),
            TitlesByYearViewModelFactory.Create(year: 2021),
            TitlesByYearViewModelFactory.Create(year: 2020),
        ];

        // Act
        var cut = Render(years, []);

        // Assert — oldest 2 years are collapsed; newest 3 are expanded
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should not collapse any year when there are 3 or fewer years")]
    public void OnParametersSet_ShouldNotCollapseAnyYear_WhenThreeOrFewerYears()
    {
        // Arrange
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2023),
            TitlesByYearViewModelFactory.Create(year: 2022),
        ];

        // Act
        var cut = Render(years, []);

        // Assert
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should not initialise collapsed years when Years is empty")]
    public void OnParametersSet_ShouldSkipInitialisation_WhenYearsIsEmpty()
    {
        // Act
        var cut = Render([], []);

        // Assert — no year sections rendered
        cut.FindAll(".year-section").Count.ShouldBe(0);
    }

    // ── ToggleYear ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should collapse an expanded year when its header is clicked")]
    public async Task ToggleYear_ShouldCollapseExpandedYear_WhenHeaderClicked()
    {
        // Arrange — single year, not collapsed by default
        var year = TitlesByYearViewModelFactory.Create(year: 2024);
        var cut = Render([year], []);
        cut.Find(".year-section").ClassList.ShouldNotContain("is-collapsed");

        // Act
        await cut.Find(".year-head").ClickAsync();

        // Assert
        cut.Find(".year-section").ClassList.ShouldContain("is-collapsed");
    }

    [Fact(DisplayName = "Should expand a collapsed year when its header is clicked")]
    public async Task ToggleYear_ShouldExpandCollapsedYear_WhenHeaderClicked()
    {
        // Arrange — 4 years; the oldest (2021) will be collapsed
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2023),
            TitlesByYearViewModelFactory.Create(year: 2022),
            TitlesByYearViewModelFactory.Create(year: 2021),
        ];
        var cut = Render(years, []);

        var yearHeads = cut.FindAll(".year-head");
        var sections = cut.FindAll(".year-section");
        sections[^1].ClassList.ShouldContain("is-collapsed");

        // Act — click the last year-head (the collapsed year, rendered in descending order)
        await yearHeads[^1].ClickAsync();

        // Assert
        cut.FindAll(".year-section")[^1].ClassList.ShouldNotContain("is-collapsed");
    }

    // ── ExpandAll / CollapseAll ───────────────────────────────────────────

    [Fact(DisplayName = "ExpandAll should remove is-collapsed from all year sections")]
    public void ExpandAll_ShouldExpandAllYears()
    {
        // Arrange — many years so some are collapsed initially
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2023),
            TitlesByYearViewModelFactory.Create(year: 2022),
            TitlesByYearViewModelFactory.Create(year: 2021),
            TitlesByYearViewModelFactory.Create(year: 2020),
        ];
        var cut = Render(years, []);
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(2);

        // Act
        cut.Instance.ExpandAll();
        cut.Render();

        // Assert
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "CollapseAll should add is-collapsed to all year sections")]
    public void CollapseAll_ShouldCollapseAllYears()
    {
        // Arrange — 2 years, both expanded
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2023),
        ];
        var cut = Render(years, []);
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(0);

        // Act
        cut.Instance.CollapseAll();
        cut.Render();

        // Assert
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(2);
    }

    // ── LookupSummary / InvokeChampion ────────────────────────────────────

    [Fact(DisplayName = "Should invoke OnBowlerSelected with matched summary when champion is clicked")]
    public async Task InvokeChampion_ShouldInvokeCallbackWithMatchedSummary_WhenSummaryExists()
    {
        // Arrange
        var summary = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Jane Smith", titleCount: 7);
        var title = BowlerTitleViewModelFactory.Create(
            bowlerId: new BowlerId(summary.BowlerId),
            bowlerName: "Jane Smith");
        var year = TitlesByYearViewModelFactory.Create(year: 2024, titles: [title]);

        BowlerTitleSummaryViewModel? captured = null;
        var callback = EventCallback.Factory.Create<BowlerTitleSummaryViewModel>(this, vm => captured = vm);

        var cut = _ctx.Render<YearView>(p => p
            .Add(c => c.Years, [year])
            .Add(c => c.Summaries, (IReadOnlyCollection<BowlerTitleSummaryViewModel>)[summary])
            .Add(c => c.OnBowlerSelected, callback));

        // Act
        await cut.Find("a.champ-link").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.BowlerName.ShouldBe("Jane Smith");
        captured.TitleCount.ShouldBe(7);
    }

    [Fact(DisplayName = "Should invoke OnBowlerSelected with empty-name summary when bowler not in summaries")]
    public async Task InvokeChampion_ShouldInvokeCallbackWithEmptyNameSummary_WhenBowlerNotInSummaries()
    {
        // Arrange — no summary for the bowler; LookupSummary returns a placeholder with BowlerName = ""
        var title = BowlerTitleViewModelFactory.Create(bowlerName: "Unknown Bowler");
        var year = TitlesByYearViewModelFactory.Create(year: 2024, titles: [title]);

        BowlerTitleSummaryViewModel? captured = null;
        var callback = EventCallback.Factory.Create<BowlerTitleSummaryViewModel>(this, vm => captured = vm);

        var cut = _ctx.Render<YearView>(p => p
            .Add(c => c.Years, [year])
            .Add(c => c.Summaries, Array.Empty<BowlerTitleSummaryViewModel>())
            .Add(c => c.OnBowlerSelected, callback));

        // Act
        await cut.Find("a.champ-link").ClickAsync();

        // Assert — LookupSummary returns a placeholder with the bowlerId set and empty name
        captured.ShouldNotBeNull();
        captured.TitleCount.ShouldBe(0);
        captured.BowlerId.ShouldBe(title.BowlerId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private IRenderedComponent<YearView> Render(
        IReadOnlyCollection<TitlesByYearViewModel> years,
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries) =>
        _ctx.Render<YearView>(p => p
            .Add(c => c.Years, years)
            .Add(c => c.Summaries, summaries)
            .Add(c => c.OnBowlerSelected, EventCallback.Factory.Create<BowlerTitleSummaryViewModel>(this, _ => { })));
}