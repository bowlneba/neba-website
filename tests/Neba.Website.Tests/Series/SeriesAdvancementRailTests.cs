using AngleSharp.Dom;

using Bunit;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementRailTests : IDisposable
{
    private const string SeriesId = SeriesDetailHost.SeriesId;

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    private IElement Render(string stageId, params StageAdvancementResponse[] stages)
        => SeriesDetailHost.Card(RenderPage(null, stages), stageId);

    private IRenderedComponent<SeriesDetail> RenderPage(DateOnly? backfillAsOf, params StageAdvancementResponse[] stages)
    {
        var advancement = SeriesAdvancementResponseFactory.Create(
            backfillAsOf: backfillAsOf,
            stages: stages);
        _host.SignInAsManager();
        _host.SetupDetailFor(advancement);
        _host.SetupAdvancement(advancement);
        return _host.Render();
    }

    // ---- Group 2: status badge ----

    [Theory(DisplayName = "Should show the Stage status in the pill with the matching class")]
    [InlineData("Pending", "series-pill--pending")]
    [InlineData("Computed", "series-pill--computed")]
    [InlineData("Official", "series-pill--official")]
    public void Status_ShouldShowTextAndPillClass(string status, string pillClass)
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: status);

        // Act
        var card = Render("q1", stage);

        // Assert
        var pill = card.QuerySelector("[data-testid='stage-status']")!;
        pill.TextContent.Trim().ShouldBe(status);
        pill.ClassList.ShouldContain("series-pill");
        pill.ClassList.ShouldContain(pillClass);
    }

    [Theory(DisplayName = "Should show a Placement Stage as Pending or Computed, never Official")]
    [InlineData("Pending")]
    [InlineData("Computed")]
    public void Status_ShouldNotShowOfficial_ForPlacementStage(string status)
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
            StageAdvancementResponseFactory.Create(stageId: "p1", order: 2, kind: "Placement", status: status),
        };

        // Act
        var card = Render("p1", stages);

        // Assert
        var pill = card.QuerySelector("[data-testid='stage-status']")!;
        pill.TextContent.Trim().ShouldBe(status);
        pill.ClassList.ShouldNotContain("series-pill--official");
    }

    [Fact(DisplayName = "Should colour each progress strip segment by status and title it with the Stage number")]
    public void Strip_ShouldFollowStatus()
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: "Pending"),
        };

        // Act
        var cut = RenderPage(null, stages);

        // Assert
        var strip = cut.Find("[data-testid='series-strip']");
        strip.GetAttribute("aria-hidden").ShouldBe("true");
        var segments = strip.QuerySelectorAll("span").ToList();
        segments.Select(s => s.GetAttribute("title")).ShouldBe(["Stage 1: Official", "Stage 2: Computed", "Stage 3: Pending"]);
        segments[0].ClassList.ShouldContain("series-strip__seg--official");
        segments[1].ClassList.ShouldContain("series-strip__seg--computed");
        segments[2].ClassList.ShouldNotContain("series-strip__seg--official");
        segments[2].ClassList.ShouldNotContain("series-strip__seg--computed");
    }

    [Fact(DisplayName = "Should give each rail node the class for its Stage status")]
    public void Node_ShouldFollowStatus()
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor", status: "Pending"),
        };

        // Act
        var cut = RenderPage(null, stages);

        // Assert
        var nodes = cut.FindAll("ol.series-rail > li").ToList();
        nodes.Count.ShouldBe(3);
        nodes[0].ClassList.ShouldContain("series-node--official");
        nodes[1].ClassList.ShouldContain("series-node--computed");
        nodes[2].ClassList.ShouldContain("series-node--pending");
        nodes.ShouldAllBe(n => !n.ClassList.Contains("series-node--cancelled"));
    }

    [Fact(DisplayName = "Should add the cancelled class to the rail node of a Stage whose tournament was cancelled")]
    public void Node_ShouldAddCancelledClass_WhenTournamentCancelled()
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, tournamentStatus: "Cancelled"),
        };

        // Act
        var cut = RenderPage(null, stages);

        // Assert
        var nodes = cut.FindAll("ol.series-rail > li").ToList();
        nodes[0].ClassList.ShouldNotContain("series-node--cancelled");
        nodes[1].ClassList.ShouldContain("series-node--pending");
        nodes[1].ClassList.ShouldContain("series-node--cancelled");
    }

    [Fact(DisplayName = "Should show the progress line with the Stage counts and the Backfill state")]
    public void ProgressLine_ShouldSummarizeStages()
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "q3", order: 3, status: "Pending"),
        };

        // Act
        var cut = RenderPage(null, stages);

        // Assert
        cut.Find("[data-testid='series-progress-line']").TextContent
            .ShouldBe("2 of 3 Stages computed or official (1 official). Backfill has not run.");
    }

    [Fact(DisplayName = "Should show a headline for a computed Stage and none for a Pending Stage")]
    public void Headline_ShouldShowPerKind_AndHideWhilePending()
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed", slotCount: 8, vacantSlotCount: 1),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed",
                survivorOutcomes:
                [
                    SurvivorOutcomeResponseFactory.Create(bowlerId: "b1", advanced: true),
                    SurvivorOutcomeResponseFactory.Create(bowlerId: "b2", advanced: false),
                ]),
            StageAdvancementResponseFactory.Create(stageId: "p1", order: 3, kind: "Placement", status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 4, status: "Pending"),
        };

        // Act
        var cut = RenderPage(null, stages);

        // Assert
        SeriesDetailHost.Card(cut, "q1").QuerySelector("[data-testid='stage-headline']")!.TextContent.ShouldBe("7 of 8 slots filled");
        SeriesDetailHost.Card(cut, "s1").QuerySelector("[data-testid='stage-headline']")!.TextContent.ShouldBe("1 advance");
        SeriesDetailHost.Card(cut, "p1").QuerySelector("[data-testid='stage-headline']")!.TextContent.ShouldBe("1 ranked");
        SeriesDetailHost.Card(cut, "q2").QuerySelector("[data-testid='stage-headline']").ShouldBeNull();
    }

    // ---- Group 3: indicators ----

    [Fact(DisplayName = "Should show the Needs squad flag alone on a Survivor Stage that needs a squad")]
    public void Flags_ShouldShowNeedsSquadAlone()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "s1", kind: "Survivor", needsSquad: true);

        // Act
        var card = Render("s1", stage);

        // Assert
        card.QuerySelector("[data-testid='stage-flag-needs-squad']")!.TextContent.ShouldBe("Needs squad");
        card.QuerySelector("[data-testid='stage-flag-stale']").ShouldBeNull();
        card.QuerySelector("[data-testid='stage-flag-due']").ShouldBeNull();
        card.QuerySelector("[data-testid='stage-flag-cancelled']").ShouldBeNull();
        card.QuerySelector("[data-testid='stage-flag-backfill']").ShouldBeNull();
        card.QuerySelector(".series-flag--warn").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should show the Stale flag alone on a stale Stage")]
    public void Flags_ShouldShowStaleAlone()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Official", isStale: true);

        // Act
        var card = Render("q1", stage);

        // Assert
        card.QuerySelector("[data-testid='stage-flag-stale']")!.TextContent.ShouldBe("Stale");
        card.QuerySelector("[data-testid='stage-flag-needs-squad']").ShouldBeNull();
        card.QuerySelector("[data-testid='stage-flag-due']").ShouldBeNull();
        card.QuerySelector("[data-testid='stage-flag-cancelled']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should show when a Computed Stage becomes official, in the viewer's time zone")]
    public void Flags_ShouldShowDueLabel_WhenComputed()
    {
        // Arrange
        var due = new DateTimeOffset(2030, 7, 12, 19, 15, 0, TimeSpan.Zero);
        _host.TimeZone.Setup(x => x.ToLocalAsync(due)).ReturnsAsync(due.AddHours(-4));
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", officialDueAt: due);

        // Act
        var card = Render("q1", stage);

        // Assert
        var flag = card.QuerySelector("[data-testid='stage-flag-due']")!;
        flag.TextContent.ShouldBe("Becomes official about 3:15 PM on 7/12");
        flag.ClassList.ShouldContain("series-flag--info");
    }

    [Theory(DisplayName = "Should not show the due label unless the Stage is Computed")]
    [InlineData("Pending")]
    [InlineData("Official")]
    public void Flags_ShouldHideDueLabel_WhenNotComputed(string status)
    {
        // Arrange
        var due = new DateTimeOffset(2030, 7, 12, 19, 15, 0, TimeSpan.Zero);
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: status, officialDueAt: due);

        // Act
        var card = Render("q1", stage);

        // Assert
        card.QuerySelector("[data-testid='stage-flag-due']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should show a cancelled flag with a link to edit the Series when the tournament was cancelled")]
    public void Flags_ShouldShowCancelledWithEditLink()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", tournamentStatus: "Cancelled");

        // Act
        var card = Render("q1", stage);

        // Assert
        var flag = card.QuerySelector("[data-testid='stage-flag-cancelled']")!;
        flag.ClassList.ShouldContain("series-flag--error");
        flag.TextContent.ShouldBe("Tournament cancelled: remove this Stage");
        flag.QuerySelector("a")!.GetAttribute("href").ShouldBe("/series/" + SeriesId + "/edit");
    }

    [Fact(DisplayName = "Should show every flag that applies when several do")]
    public void Flags_ShouldShowAllThatApply()
    {
        // Arrange
        var due = new DateTimeOffset(2030, 7, 12, 19, 15, 0, TimeSpan.Zero);
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Computed", isStale: true, officialDueAt: due),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", needsSquad: true, tournamentStatus: "Cancelled"),
        };

        // Act
        var cut = RenderPage(new DateOnly(2025, 6, 14), stages);

        // Assert
        var computed = SeriesDetailHost.Card(cut, "q1");
        computed.QuerySelector("[data-testid='stage-flag-stale']").ShouldNotBeNull();
        computed.QuerySelector("[data-testid='stage-flag-due']").ShouldNotBeNull();
        computed.QuerySelector("[data-testid='stage-flag-backfill']").ShouldNotBeNull();
        computed.QuerySelector("[data-testid='stage-flag-needs-squad']").ShouldBeNull();
        var survivor = SeriesDetailHost.Card(cut, "s1");
        survivor.QuerySelector("[data-testid='stage-flag-needs-squad']").ShouldNotBeNull();
        survivor.QuerySelector("[data-testid='stage-flag-cancelled']").ShouldNotBeNull();
        survivor.QuerySelector("[data-testid='stage-flag-stale']").ShouldBeNull();
        survivor.QuerySelector("[data-testid='stage-flag-backfill']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should show the Backfill label only on the last Qualifying Stage")]
    public void Flags_ShouldShowBackfillOnLastQualifyingOnly()
    {
        // Arrange
        var stages = new[]
        {
            StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Official"),
            StageAdvancementResponseFactory.Create(stageId: "q2", order: 2, status: "Computed"),
            StageAdvancementResponseFactory.Create(stageId: "s1", order: 3, kind: "Survivor"),
            StageAdvancementResponseFactory.Create(stageId: "p1", order: 4, kind: "Placement"),
        };

        // Act
        var cut = RenderPage(new DateOnly(2025, 6, 14), stages);

        // Assert
        SeriesDetailHost.Card(cut, "q2").QuerySelector("[data-testid='stage-flag-backfill']")!.TextContent.ShouldBe("Backfill as of 6/14/2025");
        SeriesDetailHost.Card(cut, "q1").QuerySelector("[data-testid='stage-flag-backfill']").ShouldBeNull();
        SeriesDetailHost.Card(cut, "s1").QuerySelector("[data-testid='stage-flag-backfill']").ShouldBeNull();
        SeriesDetailHost.Card(cut, "p1").QuerySelector("[data-testid='stage-flag-backfill']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should not show the Backfill label when the Backfill date is not set")]
    public void Flags_ShouldHideBackfill_WhenNoBackfillDate()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed");

        // Act
        var card = Render("q1", stage);

        // Assert
        card.QuerySelector("[data-testid='stage-flag-backfill']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should not render a flags row when no flag applies")]
    public void Flags_ShouldRenderNoRow_WhenNoneApply()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Pending");

        // Act
        var card = Render("q1", stage);

        // Assert
        card.QuerySelector(".series-flags").ShouldBeNull();
        card.QuerySelector(".series-flag").ShouldBeNull();
    }
}