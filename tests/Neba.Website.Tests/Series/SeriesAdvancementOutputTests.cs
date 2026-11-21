using AngleSharp.Dom;

using Bunit;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementOutputTests : IDisposable
{
    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    private async Task<IRenderedComponent<SeriesDetail>> RenderExpandedAsync(
        string stageId,
        IReadOnlyList<StageAdvancementResponse> stages,
        IReadOnlyList<SeriesPositionResponse>? positions = null,
        bool admin = false)
    {
        var advancement = SeriesAdvancementResponseFactory.Create(stages: stages, positions: positions ?? []);
        if (admin)
        {
            _host.SignInAsAdmin();
        }
        else
        {
            _host.SignInAsManager();
        }

        _host.SetupDetailFor(advancement);
        _host.SetupAdvancement(advancement);
        var cut = _host.Render();
        await SeriesDetailHost.ExpandAsync(cut, stageId);
        return cut;
    }

    private static IElement Output(IRenderedComponent<SeriesDetail> cut, string stageId)
        => SeriesDetailHost.Card(cut, stageId).QuerySelector("[data-testid='stage-output']")!;

    private static string[] Cells(IElement row) => [.. row.QuerySelectorAll("td").Select(td => td.TextContent.Trim())];

    // ---- Expander ----

    [Fact(DisplayName = "Should render the output panel only after the expander is pressed, with matching ARIA")]
    public async Task Expander_ShouldToggleOutputPanel()
    {
        // Arrange
        var advancement = SeriesAdvancementResponseFactory.Create(stages: [StageAdvancementResponseFactory.Create(stageId: "q1", order: 3, status: "Computed")]);
        _host.SignInAsManager();
        _host.SetupDetailFor(advancement);
        _host.SetupAdvancement(advancement);
        var cut = _host.Render();
        var button = SeriesDetailHost.Card(cut, "q1").QuerySelector("button.series-expander")!;
        var closedState = (Expanded: button.GetAttribute("aria-expanded"), Label: button.GetAttribute("aria-label"), HasPanel: SeriesDetailHost.Card(cut, "q1").QuerySelector("[data-testid='stage-output']") is not null);

        // Act
        await button.ClickAsync();

        // Assert
        closedState.ShouldBe(("false", "Show output for Stage 3", false));
        var opened = SeriesDetailHost.Card(cut, "q1");
        var openButton = opened.QuerySelector("button.series-expander")!;
        openButton.GetAttribute("aria-expanded").ShouldBe("true");
        openButton.GetAttribute("aria-label").ShouldBe("Hide output for Stage 3");
        openButton.GetAttribute("aria-controls").ShouldBe("series-output-q1");
        opened.QuerySelector("[data-testid='stage-output']")!.Id.ShouldBe("series-output-q1");
    }

    // ---- Pending output ----

    [Fact(DisplayName = "Should explain a cancelled tournament on a Pending Stage")]
    public async Task Pending_ShouldExplainCancelledTournament()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", tournamentStatus: "Cancelled");

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        Output(cut, "q1").QuerySelector("[data-testid='stage-output-pending']")!.TextContent
            .ShouldContain("This tournament was cancelled. Remove this Stage on the Edit page");
    }

    [Fact(DisplayName = "Should say a Pending Survivor Stage waits for a squad when it needs one")]
    public async Task Pending_ShouldExplainNeedsSquad()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "s1", kind: "Survivor", needsSquad: true);

        // Act
        var cut = await RenderExpandedAsync("s1", [stage]);

        // Assert
        Output(cut, "s1").QuerySelector("[data-testid='stage-output-pending']")!.TextContent
            .ShouldContain("Waiting for a squad.");
    }

    [Fact(DisplayName = "Should say positions appear when the Placement Round computes for a Pending Placement Stage")]
    public async Task Pending_ShouldExplainPlacement()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "p1", kind: "Placement");

        // Act
        var cut = await RenderExpandedAsync("p1", [stage]);

        // Assert
        Output(cut, "p1").QuerySelector("[data-testid='stage-output-pending']")!.TextContent
            .ShouldContain("Positions appear when the Placement Round computes.");
    }

    [Fact(DisplayName = "Should say a Pending Qualifying Stage is not computed yet")]
    public async Task Pending_ShouldExplainDefault()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1");

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        var output = Output(cut, "q1");
        output.QuerySelector("[data-testid='stage-output-pending']")!.TextContent
            .ShouldContain("Not computed yet. This Stage computes when its tournament completes.");
        output.QuerySelector("[data-testid='stage-entries']").ShouldBeNull();
        output.QuerySelector("[data-testid='stage-slots']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should show the stale note on an expanded stale Stage")]
    public async Task Output_ShouldShowStaleNote()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Official", isStale: true);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        Output(cut, "q1").QuerySelector("[data-testid='stage-stale-note']")!.TextContent
            .ShouldContain("Recompute from here to refresh it.");
    }

    // ---- Group 4: Qualifying output ----

    [Fact(DisplayName = "Should list field entries with place, bowler, source and Backfill points")]
    public async Task Qualifying_ShouldListEntries()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", entries:
        [
            FieldEntryResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", sourceStageId: "q1", place: 3),
            FieldEntryResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", sourceStageId: "q1", place: 5, viaOverride: true),
            FieldEntryResponseFactory.Create(bowlerId: "b3", bowlerName: "Cara Cole", backfillPoints: 250),
        ]);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        var table = Output(cut, "q1").QuerySelector("[data-testid='stage-entries']")!;
        table.GetAttribute("aria-label").ShouldBe("Field entries");
        table.ClassList.ShouldContain("neba-data-table");
        table.QuerySelectorAll("thead th").Length.ShouldBe(4);
        var rows = table.QuerySelectorAll("tbody tr").ToList();
        Cells(rows[0]).ShouldBe(["3rd", "Alice Adams", "Qualifier", "—"]);
        Cells(rows[1]).ShouldBe(["5th", "Bea Brown", "Added by admin", "—"]);
        Cells(rows[2]).ShouldBe(["—", "Cara Cole", "Backfill", "250"]);
        rows[1].QuerySelectorAll("td")[2].ClassList.ShouldContain("series-src--admin");
        rows[2].QuerySelectorAll("td")[2].ClassList.ShouldContain("series-src--backfill");
        rows[0].QuerySelectorAll("td")[2].ClassList.ShouldNotContain("series-src--admin");
    }

    [Fact(DisplayName = "Should show the slot count and the vacant slot text")]
    public async Task Qualifying_ShouldShowSlotsAndVacantText()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", slotCount: 8, vacantSlotCount: 2);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        Output(cut, "q1").QuerySelector("[data-testid='stage-slots']")!.TextContent.Trim()
            .ShouldBe("8 slots, 2 vacant slots. Vacant slots go to the Backfill.");
    }

    [Fact(DisplayName = "Should say none are vacant when every slot is filled")]
    public async Task Qualifying_ShouldSayNoneVacant()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", slotCount: 8, vacantSlotCount: 0);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        Output(cut, "q1").QuerySelector("[data-testid='stage-slots']")!.TextContent.Trim().ShouldBe("8 slots, none vacant.");
    }

    [Fact(DisplayName = "Should show the empty text when a Qualifying Stage has no field entries")]
    public async Task Qualifying_ShouldShowEmptyEntries()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", entries: []);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        var output = Output(cut, "q1");
        output.TextContent.ShouldContain("No bowlers in the field from this Stage.");
        output.QuerySelector("[data-testid='stage-entries']").ShouldBeNull();
    }

    [Fact(DisplayName = "Should list overrides with change, bowler, reason, date and status, and mark one that no longer applies")]
    public async Task Qualifying_ShouldListOverrides()
    {
        // Arrange
        var created = new DateTimeOffset(2025, 3, 25, 12, 0, 0, TimeSpan.Zero);
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", overrides:
        [
            StageOverrideResponseFactory.Create(overrideId: "o1", bowlerId: "b2", bowlerName: "Bea Brown", kind: "Add", reason: "Late entry approved.", createdAt: created, applied: true),
            StageOverrideResponseFactory.Create(overrideId: "o2", bowlerId: "b9", bowlerName: "Zed Zane", kind: "Clear", reason: "Withdrew.", createdAt: created, applied: false),
        ]);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        var table = Output(cut, "q1").QuerySelector("[data-testid='stage-overrides']")!;
        table.GetAttribute("aria-label").ShouldBe("Admin overrides");
        table.ClassList.ShouldContain("neba-data-table");
        table.QuerySelectorAll("thead th").Length.ShouldBe(5);
        var rows = table.QuerySelectorAll("tbody tr").ToList();
        Cells(rows[0]).ShouldBe(["Add bowler", "Bea Brown", "Late entry approved.", "3/25/2025", "Applied"]);
        Cells(rows[1]).ShouldBe(["Clear slot", "Zed Zane", "Withdrew.", "3/25/2025", "No longer applies"]);
        rows[0].QuerySelector(".series-output__inactive").ShouldBeNull();
        rows[1].QuerySelector(".series-output__inactive")!.TextContent.ShouldBe("No longer applies");
    }

    [Fact(DisplayName = "Should not render the overrides table when the Stage has no overrides")]
    public async Task Qualifying_ShouldHideOverrides_WhenNone()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", overrides: []);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage]);

        // Assert
        Output(cut, "q1").QuerySelector("[data-testid='stage-overrides']").ShouldBeNull();
    }

    [Theory(DisplayName = "Should render no Add bowler, Clear slot or Remove override control for a Manager or an Admin")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Qualifying_ShouldRenderNoOverrideControls(bool admin)
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "q1", status: "Computed", overrides: [StageOverrideResponseFactory.Create()], entries:
        [
            FieldEntryResponseFactory.Create(bowlerId: "b1", sourceStageId: "q1", place: 1),
        ]);

        // Act
        var cut = await RenderExpandedAsync("q1", [stage], admin: admin);

        // Assert
        var buttonTexts = cut.FindAll("button").Select(b => b.TextContent).ToList();
        buttonTexts.ShouldNotContain(t => t.Contains("Add bowler", StringComparison.Ordinal));
        buttonTexts.ShouldNotContain(t => t.Contains("Clear slot", StringComparison.Ordinal));
        buttonTexts.ShouldNotContain(t => t.Contains("Remove override", StringComparison.Ordinal));
        Output(cut, "q1").QuerySelectorAll("button").ShouldBeEmpty();
    }

    // ---- Group 5: Survivor output ----

    [Fact(DisplayName = "Should show the Survivor summary, 'No full block' and the out class for bowlers who did not advance")]
    public async Task Survivor_ShouldShowSummaryAndOutcomes()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "s1", kind: "Survivor", status: "Computed", survivorOutcomes:
        [
            SurvivorOutcomeResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", blockTotal: 1500, advanced: true),
            SurvivorOutcomeResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", blockTotal: 1400, advanced: true),
            SurvivorOutcomeResponseFactory.Create(bowlerId: "b3", bowlerName: "Cara Cole", blockTotal: 1300, advanced: false),
            SurvivorOutcomeResponseFactory.Create(bowlerId: "b4", bowlerName: "Dee Dunn", blockTotal: null, advanced: false),
        ]);

        // Act
        var cut = await RenderExpandedAsync("s1", [stage]);

        // Assert
        var output = Output(cut, "s1");
        output.QuerySelector("[data-testid='stage-survivor-summary']")!.TextContent
            .ShouldBe("2 advance of 4 in the field. 3 bowled; 1 did not.");
        var table = output.QuerySelector("[data-testid='stage-outcomes']")!;
        table.GetAttribute("aria-label").ShouldBe("Survivor results");
        table.ClassList.ShouldNotContain("neba-data-table");
        table.QuerySelectorAll("thead th").Length.ShouldBe(3);
        var rows = table.QuerySelectorAll("tbody tr").ToList();
        Cells(rows[0]).ShouldBe(["Alice Adams", "1500", "Yes"]);
        Cells(rows[2]).ShouldBe(["Cara Cole", "1300", "No"]);
        Cells(rows[3]).ShouldBe(["Dee Dunn", "No full block", "No"]);
        rows.Select(r => r.ClassList.Contains("series-output__out")).ShouldBe([false, false, true, true]);
    }

    [Fact(DisplayName = "Should show the empty text when a computed Survivor Stage has no outcomes")]
    public async Task Survivor_ShouldShowEmptyState()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "s1", kind: "Survivor", status: "Computed", survivorOutcomes: []);

        // Act
        var cut = await RenderExpandedAsync("s1", [stage]);

        // Assert
        var output = Output(cut, "s1");
        output.TextContent.ShouldContain("No bowlers in the field from this Stage.");
        output.QuerySelector("[data-testid='stage-outcomes']").ShouldBeNull();
        output.QuerySelector("[data-testid='stage-survivor-summary']").ShouldBeNull();
    }

    // ---- Group 6: Placement output ----

    [Fact(DisplayName = "Should show the Finalist column and the phone table class when a position has a Finalist flag")]
    public async Task Placement_ShouldShowFinalistColumn_WhenFinale()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "p1", kind: "Placement", status: "Computed");
        SeriesPositionResponse[] positions =
        [
            SeriesPositionResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", position: 1, bowled: true, isFinalist: true),
            SeriesPositionResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", position: 2, bowled: false, isFinalist: false),
        ];

        // Act
        var cut = await RenderExpandedAsync("p1", [stage], positions);

        // Assert
        var output = Output(cut, "p1");
        output.QuerySelector("[data-testid='stage-placement-summary']")!.TextContent
            .ShouldBe("2 bowlers ranked. Finalist positions are provisional until the Finale is recorded.");
        var table = output.QuerySelector("[data-testid='stage-positions']")!;
        table.GetAttribute("aria-label").ShouldBe("Placement positions");
        table.ClassList.ShouldContain("neba-data-table");
        table.QuerySelectorAll("thead th").Select(th => th.TextContent).ShouldBe(["Position", "Bowler", "Bowled", "Finalist"]);
        var rows = table.QuerySelectorAll("tbody tr").ToList();
        Cells(rows[0]).ShouldBe(["1st", "Alice Adams", "Yes", "Yes"]);
        Cells(rows[1]).ShouldBe(["2nd", "Bea Brown", "No", "No"]);
    }

    [Fact(DisplayName = "Should omit the Finalist column and the phone table class when no position has a Finalist flag")]
    public async Task Placement_ShouldOmitFinalistColumn_WhenNoFinale()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "p1", kind: "Placement", status: "Computed");
        SeriesPositionResponse[] positions =
        [
            SeriesPositionResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", position: 1),
            SeriesPositionResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", position: 2),
        ];

        // Act
        var cut = await RenderExpandedAsync("p1", [stage], positions);

        // Assert
        var output = Output(cut, "p1");
        output.QuerySelector("[data-testid='stage-placement-summary']")!.TextContent
            .ShouldBe("2 bowlers ranked. These positions are final.");
        var table = output.QuerySelector("[data-testid='stage-positions']")!;
        table.ClassList.ShouldNotContain("neba-data-table");
        table.QuerySelectorAll("thead th").Select(th => th.TextContent).ShouldBe(["Position", "Bowler", "Bowled"]);
        Cells(table.QuerySelectorAll("tbody tr")[0]).ShouldBe(["1st", "Alice Adams", "Yes"]);
    }

    [Fact(DisplayName = "Should show the positions-appear text when a computed Placement Stage has no positions")]
    public async Task Placement_ShouldShowEmptyState_WhenNoPositions()
    {
        // Arrange
        var stage = StageAdvancementResponseFactory.Create(stageId: "p1", kind: "Placement", status: "Computed");

        // Act
        var cut = await RenderExpandedAsync("p1", [stage], positions: []);

        // Assert
        var output = Output(cut, "p1");
        output.TextContent.ShouldContain("Positions appear when the Placement Round computes.");
        output.QuerySelector("[data-testid='stage-positions']").ShouldBeNull();
    }
}