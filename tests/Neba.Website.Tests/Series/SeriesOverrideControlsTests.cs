using AngleSharp.Dom;

using Bunit;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesOverrideControlsTests : IDisposable
{
    private const string AddButton = "[data-testid='stage-add-bowler']";
    private const string ClearButton = "[data-testid='entry-clear']";
    private const string RemoveButton = "[data-testid='override-remove']";

    private readonly SeriesDetailHost _host = new();

    public void Dispose() => _host.Dispose();

    private static StageAdvancementResponse QualifyingWithOutput(string stageId, int order, string status = "Computed")
        => StageAdvancementResponseFactory.Create(
            stageId: stageId,
            order: order,
            status: status,
            entries:
            [
                FieldEntryResponseFactory.Create(bowlerId: "b1", bowlerName: "Alice Adams", sourceStageId: stageId, place: 1),
                FieldEntryResponseFactory.Create(bowlerId: "b2", bowlerName: "Bea Brown", sourceStageId: stageId, place: 2, viaOverride: true),
                FieldEntryResponseFactory.Create(bowlerId: "b3", bowlerName: "Cara Cole"),
            ],
            overrides:
            [
                StageOverrideResponseFactory.Create(overrideId: "ov-add", bowlerId: "b2", bowlerName: "Bea Brown", kind: "Add"),
                StageOverrideResponseFactory.Create(overrideId: "ov-clear", bowlerId: "b9", bowlerName: "Zed Zane", kind: "Clear"),
            ]);

    private static SeriesAdvancementResponse AllKinds()
        => SeriesAdvancementResponseFactory.Create(
            stages:
            [
                QualifyingWithOutput("q1", 1),
                StageAdvancementResponseFactory.Create(stageId: "s1", order: 2, kind: "Survivor", status: "Computed"),
                StageAdvancementResponseFactory.Create(stageId: "p1", order: 3, kind: "Placement", status: "Computed"),
            ]);

    private IRenderedComponent<SeriesDetail> RenderPage(SeriesAdvancementResponse advancement, Action signIn)
    {
        signIn();
        _host.SetupDetailFor(advancement);
        _host.SetupAdvancement(advancement);
        return _host.Render();
    }

    private static IElement Output(IRenderedComponent<SeriesDetail> cut, string stageId)
        => SeriesDetailHost.Card(cut, stageId).QuerySelector("[data-testid='stage-output']")!;

    private static int Headers(IElement table) => table.QuerySelectorAll("thead th").Length;

    private static bool HasActionsHeader(IElement table)
        => table.QuerySelectorAll("thead th").Any(th => th.TextContent.Trim() == "Actions");

    // ---- Who sees the controls ----

    [Fact(DisplayName = "Should render Add bowler, Clear slot and Remove override with their hooks and labels for a holder of Series.ManageAdvancement")]
    public async Task Controls_ShouldRender_ForManageAdvancementHolder()
    {
        // Arrange
        var cut = RenderPage(AllKinds(), _host.SignInAsAdmin);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Assert
        var output = Output(cut, "q1");
        var add = output.QuerySelector(AddButton).ShouldNotBeNull();
        add.TextContent.Trim().ShouldBe("Add bowler");
        add.GetAttribute("aria-label").ShouldBe("Add bowler to the 1st tournament");
        add.HasAttribute("disabled").ShouldBeFalse();
        var clear = output.QuerySelectorAll(ClearButton).ShouldHaveSingleItem();
        clear.GetAttribute("data-bowler-id").ShouldBe("b1");
        clear.GetAttribute("aria-label").ShouldBe("Clear slot for Alice Adams");
        clear.TextContent.Trim().ShouldBe("Clear slot");
        var removes = output.QuerySelectorAll(RemoveButton);
        removes.Select(r => r.GetAttribute("data-override-id")).ShouldBe(["ov-add", "ov-clear"]);
        removes.Select(r => r.GetAttribute("aria-label")).ShouldBe(["Remove override for Bea Brown", "Remove override for Zed Zane"]);
        removes.ShouldAllBe(r => r.TextContent.Trim() == "Remove override" && !r.HasAttribute("disabled"));
    }

    [Fact(DisplayName = "Should render no control and no Actions column header for a Manager")]
    public async Task Controls_ShouldBeAbsent_ForManager()
    {
        // Arrange
        var cut = RenderPage(AllKinds(), _host.SignInAsManager);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Assert
        var output = Output(cut, "q1");
        output.QuerySelector(AddButton).ShouldBeNull();
        output.QuerySelector(ClearButton).ShouldBeNull();
        output.QuerySelector(RemoveButton).ShouldBeNull();
        output.QuerySelector("[data-testid='stage-entries']").ShouldNotBeNull();
        output.QuerySelector("[data-testid='stage-overrides']").ShouldNotBeNull();
        output.QuerySelectorAll("th").Any(th => th.TextContent.Trim() == "Actions").ShouldBeFalse();
    }

    [Fact(DisplayName = "Should render no control and make no advancement call for an anonymous visitor")]
    public void Controls_ShouldBeAbsent_ForAnonymous()
    {
        // Arrange
        _host.SetupDetailFor(AllKinds());

        // Act
        var cut = _host.Render();

        // Assert
        cut.FindAll(AddButton).ShouldBeEmpty();
        cut.FindAll(ClearButton).ShouldBeEmpty();
        cut.FindAll(RemoveButton).ShouldBeEmpty();
        cut.FindAll("[data-testid='stage-output']").ShouldBeEmpty();
        _host.VerifyAdvancementCalls(Times.Never());
        _host.VerifyAnyResultsCalls(Times.Never());
    }

    // ---- Where the controls show ----

    [Fact(DisplayName = "Should render no override control on an Official Qualifying Stage")]
    public async Task Controls_ShouldBeAbsent_OnOfficialStage()
    {
        // Arrange
        var advancement = SeriesAdvancementResponseFactory.Create(stages: [QualifyingWithOutput("q1", 1, "Official")]);
        var cut = RenderPage(advancement, _host.SignInAsAdmin);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Assert
        var output = Output(cut, "q1");
        output.QuerySelector("[data-testid='stage-entries']").ShouldNotBeNull();
        output.QuerySelector("[data-testid='stage-overrides']").ShouldNotBeNull();
        output.QuerySelectorAll("button").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render no override control on a Pending Qualifying Stage")]
    public async Task Controls_ShouldBeAbsent_OnPendingStage()
    {
        // Arrange
        var advancement = SeriesAdvancementResponseFactory.Create(stages: [StageAdvancementResponseFactory.Create(stageId: "q1", order: 1, status: "Pending")]);
        var cut = RenderPage(advancement, _host.SignInAsAdmin);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Assert
        Output(cut, "q1").QuerySelector("[data-testid='stage-output-pending']").ShouldNotBeNull();
        Output(cut, "q1").QuerySelectorAll("button").ShouldBeEmpty();
        cut.FindAll(AddButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render no override control on a Survivor or a Placement Stage")]
    public async Task Controls_ShouldBeAbsent_OnSurvivorAndPlacementStages()
    {
        // Arrange
        var cut = RenderPage(AllKinds(), _host.SignInAsAdmin);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "s1");
        await SeriesDetailHost.ExpandAsync(cut, "p1");

        // Assert
        Output(cut, "s1").QuerySelectorAll("button").ShouldBeEmpty();
        Output(cut, "p1").QuerySelectorAll("button").ShouldBeEmpty();
        Output(cut, "s1").QuerySelectorAll("th").Any(th => th.TextContent.Trim() == "Actions").ShouldBeFalse();
        Output(cut, "p1").QuerySelectorAll("th").Any(th => th.TextContent.Trim() == "Actions").ShouldBeFalse();
        cut.FindAll(AddButton).ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render Clear slot only on a row the Stage itself qualified, not on an override row or a Backfill row")]
    public async Task Clear_ShouldBeAbsent_OnOverrideAndBackfillRows()
    {
        // Arrange
        var cut = RenderPage(AllKinds(), _host.SignInAsAdmin);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Assert
        var rows = Output(cut, "q1").QuerySelectorAll("[data-testid='stage-entries'] tbody tr").ToList();
        rows.Count.ShouldBe(3);
        rows[0].QuerySelector(ClearButton).ShouldNotBeNull();
        rows[1].QuerySelector(ClearButton).ShouldBeNull();
        rows[2].QuerySelector(ClearButton).ShouldBeNull();
    }

    [Fact(DisplayName = "Should make no results call for a Manager even after every Stage expands")]
    public async Task Manager_ShouldMakeNoResultsCall_AfterExpandingStages()
    {
        // Arrange
        var cut = RenderPage(AllKinds(), _host.SignInAsManager);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");
        await SeriesDetailHost.ExpandAsync(cut, "s1");
        await SeriesDetailHost.ExpandAsync(cut, "p1");

        // Assert
        _host.VerifyAnyResultsCalls(Times.Never());
        _host.VerifyAdvancementCalls(Times.Once());
    }

    [Fact(DisplayName = "Should make no results call for an Admin until Add bowler is pressed")]
    public async Task Admin_ShouldMakeNoResultsCall_UntilAddBowlerIsPressed()
    {
        // Arrange
        var cut = RenderPage(AllKinds(), _host.SignInAsAdmin);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Assert
        _host.VerifyAnyResultsCalls(Times.Never());
        cut.FindAll("[data-testid='stage-override-modal']").ShouldBeEmpty();
    }

    // ---- Table shape ----

    [Theory(DisplayName = "Should give every row as many cells as the table has headers, with the Actions column only for CanManage")]
    [InlineData(false, 4, 5)]
    [InlineData(true, 5, 6)]
    public async Task Tables_ShouldMatchHeaderCounts(bool admin, int entryColumns, int overrideColumns)
    {
        // Arrange
        var cut = RenderPage(AllKinds(), admin ? _host.SignInAsAdmin : _host.SignInAsManager);

        // Act
        await SeriesDetailHost.ExpandAsync(cut, "q1");

        // Assert
        var entries = Output(cut, "q1").QuerySelector("[data-testid='stage-entries']").ShouldNotBeNull();
        var overrides = Output(cut, "q1").QuerySelector("[data-testid='stage-overrides']").ShouldNotBeNull();
        Headers(entries).ShouldBe(entryColumns);
        Headers(overrides).ShouldBe(overrideColumns);
        HasActionsHeader(entries).ShouldBe(admin);
        HasActionsHeader(overrides).ShouldBe(admin);
        entries.QuerySelectorAll("tbody tr").ShouldAllBe(row => row.QuerySelectorAll("td").Length == entryColumns);
        overrides.QuerySelectorAll("tbody tr").ShouldAllBe(row => row.QuerySelectorAll("td").Length == overrideColumns);
    }
}