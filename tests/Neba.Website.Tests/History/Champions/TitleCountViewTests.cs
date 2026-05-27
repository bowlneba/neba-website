using System.Globalization;

using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Champions;
using Neba.Website.Server.History.Champions;

namespace Neba.Website.Tests.History.Champions;

[UnitTest]
[Component("Website.History.Champions.TitleCountView")]
public sealed class TitleCountViewTests : IDisposable
{
    private readonly BunitContext _ctx;

    public TitleCountViewTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    // ── Tier filtering ────────────────────────────────────────────────────

    [Fact(DisplayName = "Should render all three tier sections")]
    public void Render_ShouldRenderAllThreeTierSections()
    {
        // Act
        var cut = Render([]);

        // Assert
        cut.FindAll(".tier-section").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should place bowler with 20+ titles in the elite tier")]
    public void Render_ShouldPlaceBowlerInEliteTier_WhenTitleCountIsAtLeast20()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Elite Bowler", titleCount: 25);

        // Act
        var cut = Render([bowler]);

        // Assert — elite tier contains the bowler card
        var eliteSection = cut.Find("[data-tier=\"elite\"]");
        eliteSection.TextContent.ShouldContain("Elite Bowler");
    }

    [Fact(DisplayName = "Should place bowler with 10-19 titles in the mid (decorated) tier")]
    public void Render_ShouldPlaceBowlerInMidTier_WhenTitleCountIsBetween10And19()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Mid Bowler", titleCount: 15);

        // Act
        var cut = Render([bowler]);

        // Assert
        var midSection = cut.Find("[data-tier=\"mid\"]");
        midSection.TextContent.ShouldContain("Mid Bowler");
    }

    [Fact(DisplayName = "Should place bowler with 1-9 titles in the standard (champions) tier")]
    public void Render_ShouldPlaceBowlerInStdTier_WhenTitleCountIsBetween1And9()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Std Bowler", titleCount: 5);

        // Act
        var cut = Render([bowler]);

        // Assert
        var stdSection = cut.Find("[data-tier=\"std\"]");
        stdSection.TextContent.ShouldContain("Std Bowler");
    }

    [Fact(DisplayName = "Should show HOF badge when bowler is in Hall of Fame")]
    public void Render_ShouldShowHofBadge_WhenBowlerIsInHallOfFame()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(titleCount: 5, hallOfFame: true);

        // Act
        var cut = Render([bowler]);

        // Assert
        cut.Markup.ShouldContain("neba-hof.jpg");
    }

    [Fact(DisplayName = "Should not show HOF badge when bowler is not in Hall of Fame")]
    public void Render_ShouldNotShowHofBadge_WhenBowlerIsNotInHallOfFame()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(titleCount: 5, hallOfFame: false);

        // Act
        var cut = Render([bowler]);

        // Assert
        cut.Markup.ShouldNotContain("neba-hof.jpg");
    }

    [Fact(DisplayName = "Should display 'title' (singular) when count group has exactly 1 title")]
    public void Render_ShouldUseSingularTitleLabel_WhenTitleCountIsOne()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(titleCount: 1);

        // Act
        var cut = Render([bowler]);

        // Assert — 'title' appears without 's'
        var countGroup = cut.Find("[data-tier=\"std\"] .count-group__label");
        countGroup.TextContent.Trim().ShouldBe("title");
    }

    [Fact(DisplayName = "Should display 'titles' (plural) when count group has more than 1 title")]
    public void Render_ShouldUsePluralTitleLabel_WhenTitleCountIsGreaterThanOne()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(titleCount: 5);

        // Act
        var cut = Render([bowler]);

        // Assert
        var countGroup = cut.Find("[data-tier=\"std\"] .count-group__label");
        countGroup.TextContent.Trim().ShouldBe("titles");
    }

    [Fact(DisplayName = "Should display 'bowler' (singular) in tally when group has exactly 1 bowler")]
    public void Render_ShouldUseSingularBowlerLabel_WhenGroupHasOneBowler()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(titleCount: 5);

        // Act
        var cut = Render([bowler]);

        // Assert
        var countTally = cut.Find("[data-tier=\"std\"] .count-group__tally");
        countTally.TextContent.ShouldContain("bowler");
        countTally.TextContent.ShouldNotContain("bowlers");
    }

    [Fact(DisplayName = "Should display 'bowlers' (plural) in tally when group has multiple bowlers")]
    public void Render_ShouldUsePluralBowlerLabel_WhenGroupHasMultipleBowlers()
    {
        // Arrange — two bowlers with the same title count so they land in the same group
        var bowler1 = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Alice", titleCount: 5);
        var bowler2 = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Bob", titleCount: 5);

        // Act
        var cut = Render([bowler1, bowler2]);

        // Assert
        var countTally = cut.Find("[data-tier=\"std\"] .count-group__tally");
        countTally.TextContent.ShouldContain("bowlers");
    }

    // ── ToggleTier ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should collapse the elite tier when its header is clicked")]
    public async Task ToggleTier_ShouldCollapseTier_WhenHeaderIsClicked()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(titleCount: 25);
        var cut = Render([bowler]);
        cut.Find("[data-tier=\"elite\"]").ClassList.ShouldNotContain("is-collapsed");

        // Act
        await cut.Find("[data-tier=\"elite\"] .tier-head").ClickAsync();

        // Assert
        cut.Find("[data-tier=\"elite\"]").ClassList.ShouldContain("is-collapsed");
    }

    [Fact(DisplayName = "Should expand the elite tier when its header is clicked while collapsed")]
    public async Task ToggleTier_ShouldExpandTier_WhenClickedWhileCollapsed()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(titleCount: 25);
        var cut = Render([bowler]);

        // Collapse first
        await cut.Find("[data-tier=\"elite\"] .tier-head").ClickAsync();
        cut.Find("[data-tier=\"elite\"]").ClassList.ShouldContain("is-collapsed");

        // Act — click again to expand
        await cut.Find("[data-tier=\"elite\"] .tier-head").ClickAsync();

        // Assert
        cut.Find("[data-tier=\"elite\"]").ClassList.ShouldNotContain("is-collapsed");
    }

    // ── ExpandAll / CollapseAll ───────────────────────────────────────────

    [Fact(DisplayName = "CollapseAll should add is-collapsed to all three tiers")]
    public void CollapseAll_ShouldCollapseAllTiers()
    {
        // Arrange
        var cut = Render([]);
        cut.FindAll(".tier-section.is-collapsed").Count.ShouldBe(0);

        // Act
        cut.Instance.CollapseAll();
        cut.Render();

        // Assert
        cut.FindAll(".tier-section.is-collapsed").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "ExpandAll should remove is-collapsed from all three tiers")]
    public void ExpandAll_ShouldExpandAllTiers()
    {
        // Arrange — collapse all first
        var cut = Render([]);
        cut.Instance.CollapseAll();
        cut.Render();
        cut.FindAll(".tier-section.is-collapsed").Count.ShouldBe(3);

        // Act
        cut.Instance.ExpandAll();
        cut.Render();

        // Assert
        cut.FindAll(".tier-section.is-collapsed").Count.ShouldBe(0);
    }

    // ── OnBowlerSelected callback ─────────────────────────────────────────

    [Fact(DisplayName = "Should invoke OnBowlerSelected when a bowler card is clicked")]
    public async Task Render_ShouldInvokeOnBowlerSelected_WhenBowlerCardIsClicked()
    {
        // Arrange
        var bowler = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Jane Smith", titleCount: 5);

        BowlerTitleSummaryViewModel? captured = null;
        var callback = EventCallback.Factory.Create<BowlerTitleSummaryViewModel>(this, vm => captured = vm);

        var cut = _ctx.Render<TitleCountView>(p => p
            .Add(c => c.Summaries, (IReadOnlyCollection<BowlerTitleSummaryViewModel>)[bowler])
            .Add(c => c.OnBowlerSelected, callback));

        // Act
        await cut.Find("button.bowler-card").ClickAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.BowlerName.ShouldBe("Jane Smith");
    }

    // ── GroupByCount ordering ────────────────────────────────────────────

    [Fact(DisplayName = "Should group bowlers with the same title count and render in descending count order")]
    public void Render_ShouldGroupAndOrderBowlersByTitleCountDescending()
    {
        // Arrange
        var bowlerA = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Alice", titleCount: 3);
        var bowlerB = BowlerTitleSummaryViewModelFactory.Create(bowlerName: "Bob", titleCount: 7);

        // Act
        var cut = Render([bowlerA, bowlerB]);

        // Assert — 7-title group appears before 3-title group in the std tier
        var countNums = cut.FindAll("[data-tier=\"std\"] .count-group__num")
            .Select(e => int.Parse(e.TextContent.Trim(), CultureInfo.InvariantCulture))
            .ToList();
        countNums[0].ShouldBeGreaterThan(countNums[1]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private IRenderedComponent<TitleCountView> Render(
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries) =>
        _ctx.Render<TitleCountView>(p => p
            .Add(c => c.Summaries, summaries)
            .Add(c => c.OnBowlerSelected, EventCallback.Factory.Create<BowlerTitleSummaryViewModel>(this, _ => { })));
}