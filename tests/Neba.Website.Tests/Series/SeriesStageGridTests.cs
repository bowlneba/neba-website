using Bunit;

using Microsoft.AspNetCore.Components;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Series;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesStageGridTests : IDisposable
{
    private static readonly DateOnly Day1 = new(2025, 5, 31);
    private static readonly DateOnly Day2 = new(2025, 7, 12);

    private readonly BunitContext _ctx = new();
    private int _changes;

    public SeriesStageGridTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    private static IReadOnlyList<SeriesTournamentOption> Options() =>
    [
        SeriesTournamentOptionFactory.Create(id: "t1", label: "Sat 5/31 · Westgate Lanes", startDate: Day1),
        SeriesTournamentOptionFactory.Create(id: "t2", label: "Sat 7/12 · Callahan's", startDate: Day2),
        SeriesTournamentOptionFactory.Create(id: "t3", label: "Sun 8/3 · Team Event", startDate: new DateOnly(2025, 8, 3), teamSize: 3),
    ];

    private IRenderedComponent<SeriesStageGrid> Render(
        List<SeriesStageRowModel> rows, bool readOnly = false, int heldElsewhere = 0)
        => _ctx.Render<SeriesStageGrid>(p => p
            .Add(x => x.Rows, rows)
            .Add(x => x.Options, Options())
            .Add(x => x.ReadOnly, readOnly)
            .Add(x => x.HeldElsewhere, heldElsewhere)
            .Add(x => x.OnChanged, EventCallback.Factory.Create(this, () => _changes++)));

    [Fact(DisplayName = "Add Stage should append a row and raise OnChanged")]
    public async Task Add_ShouldAppendRow()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [];
        var cut = Render(rows);

        // Act
        await cut.Find(".series-grid__foot button").ClickAsync();

        // Assert
        rows.Count.ShouldBe(1);
        cut.FindAll("li.series-grid__row").Count.ShouldBe(1);
        _changes.ShouldBe(1);
    }

    [Fact(DisplayName = "Remove should drop the row and raise OnChanged")]
    public async Task Remove_ShouldDropRow()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { TournamentId = "t1" }, new() { TournamentId = "t2" }];
        var cut = Render(rows);

        // Act
        await cut.Find("button[aria-label='Stage 1 remove']").ClickAsync();

        // Assert
        rows.Select(r => r.TournamentId).ShouldBe(["t2"]);
        _changes.ShouldBe(1);
    }

    [Fact(DisplayName = "Move up and down should swap rows, and the first and last buttons are disabled")]
    public async Task Move_ShouldSwapRows_AndDisableEnds()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { TournamentId = "t1" }, new() { TournamentId = "t2" }];
        var cut = Render(rows);

        // Assert (before)
        cut.Find("button[aria-label='Stage 1 move up']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("button[aria-label='Stage 2 move down']").HasAttribute("disabled").ShouldBeTrue();

        // Act
        await cut.Find("button[aria-label='Stage 1 move down']").ClickAsync();

        // Assert
        rows.Select(r => r.TournamentId).ShouldBe(["t2", "t1"]);

        // Act
        await cut.Find("button[aria-label='Stage 2 move up']").ClickAsync();

        // Assert
        rows.Select(r => r.TournamentId).ShouldBe(["t1", "t2"]);
        _changes.ShouldBe(2);
    }

    [Fact(DisplayName = "Switching to Survivor should set the squad date to the tournament start, and switching away clears it")]
    public async Task Kind_ShouldSetAndClearSquad()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { TournamentId = "t2" }];
        var cut = Render(rows);

        // Act
        await cut.Find("#stage-0-kind").ChangeAsync(new ChangeEventArgs { Value = StageKinds.Survivor });

        // Assert
        rows[0].SquadDate.ShouldBe(Day2);

        // Act
        rows[0].SquadStartTime = new TimeOnly(9, 0);
        await cut.Find("#stage-0-kind").ChangeAsync(new ChangeEventArgs { Value = StageKinds.Qualifying });

        // Assert
        rows[0].SquadDate.ShouldBeNull();
        rows[0].SquadStartTime.ShouldBeNull();
    }

    [Fact(DisplayName = "Choosing a tournament on a Survivor row should set the squad date to its start")]
    public async Task Tournament_ShouldSetSquadDate_ForSurvivor()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { Kind = StageKinds.Survivor }];
        var cut = Render(rows);

        // Act
        await cut.Find("#stage-0-tournament").ChangeAsync(new ChangeEventArgs { Value = "t1" });

        // Assert
        rows[0].TournamentId.ShouldBe("t1");
        rows[0].SquadDate.ShouldBe(Day1);
    }

    [Fact(DisplayName = "A tournament picked on one row should drop out of the other rows' lists")]
    public void Tournament_ShouldDropOutOfOtherRows()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { TournamentId = "t1" }, new()];

        // Act
        var cut = Render(rows);

        // Assert
        var row1 = cut.Find("#stage-0-tournament").QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToList();
        var row2 = cut.Find("#stage-1-tournament").QuerySelectorAll("option").Select(o => o.GetAttribute("value")).ToList();
        row1.ShouldContain("t1");
        row2.ShouldNotContain("t1");
        row2.ShouldContain("t2");
    }

    [Fact(DisplayName = "The team hint should follow the count")]
    public async Task Count_ShouldUpdateTeamHint()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { TournamentId = "t3", AdvancingCount = 2 }];
        var cut = Render(rows);

        // Assert
        cut.Find(".series-editor__hint").TextContent.ShouldContain("Teams of 3");
        cut.Find(".series-editor__hint").TextContent.ShouldContain("(6 bowlers)");

        // Act
        await cut.Find("#stage-0-count").ChangeAsync(new ChangeEventArgs { Value = "4" });

        // Assert
        cut.Find(".series-editor__hint").TextContent.ShouldContain("(12 bowlers)");
    }

    [Fact(DisplayName = "A Placement row should show 'No count' and no count input")]
    public void Placement_ShouldShowNoCount()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { Kind = StageKinds.Placement, AdvancingCount = null }];

        // Act
        var cut = Render(rows);

        // Assert
        cut.Find(".series-grid__no-count").TextContent.ShouldBe("No count");
        cut.FindAll("#stage-0-count").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Switching a row to Placement should clear its count")]
    public async Task Kind_ShouldClearCount_ForPlacement()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { AdvancingCount = 8 }];
        var cut = Render(rows);

        // Act
        await cut.Find("#stage-0-kind").ChangeAsync(new ChangeEventArgs { Value = StageKinds.Placement });

        // Assert
        rows[0].AdvancingCount.ShouldBeNull();
    }

    [Fact(DisplayName = "A cleared count should become null")]
    public async Task Count_ShouldBeNull_WhenCleared()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { AdvancingCount = 8 }];
        var cut = Render(rows);

        // Act
        await cut.Find("#stage-0-count").ChangeAsync(new ChangeEventArgs { Value = string.Empty });

        // Assert
        rows[0].AdvancingCount.ShouldBeNull();
    }

    [Fact(DisplayName = "Row errors should render under the right row, and a move clears them")]
    public async Task Errors_ShouldRenderUnderRow_AndClearOnMove()
    {
        // Arrange
        var second = new SeriesStageRowModel { TournamentId = "t2" };
        second.Errors.Add("Pick a later tournament.");
        List<SeriesStageRowModel> rows = [new() { TournamentId = "t1" }, second];
        var cut = Render(rows);

        // Assert
        var items = cut.FindAll("li.series-grid__row");
        items[0].QuerySelectorAll(".series-grid__error").ShouldBeEmpty();
        items[1].QuerySelector(".series-grid__error")!.TextContent.ShouldBe("Pick a later tournament.");
        items[1].ClassList.ShouldContain("series-grid__row--error");

        // Act
        await cut.Find("button[aria-label='Stage 2 move up']").ClickAsync();

        // Assert
        cut.FindAll(".series-grid__error").ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadOnly should render text with no inputs")]
    public void ReadOnly_ShouldRenderText()
    {
        // Arrange
        List<SeriesStageRowModel> rows =
        [
            new() { TournamentId = "t1", AdvancingCount = 8 },
            new() { Kind = StageKinds.Placement, TournamentId = "t2", AdvancingCount = null },
        ];

        // Act
        var cut = Render(rows, readOnly: true);

        // Assert
        cut.FindAll("input, select, button").ShouldBeEmpty();
        cut.Markup.ShouldContain("Westgate Lanes");
        cut.Markup.ShouldContain("Top 8");
        cut.Markup.ShouldContain("Final standings");
    }

    [Fact(DisplayName = "Should note tournaments held by another Series")]
    public void HeldElsewhere_ShouldShowNote()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [];

        // Act
        var cut = Render(rows, heldElsewhere: 2);

        // Assert
        cut.Find(".series-grid__foot .series-editor__hint").TextContent
            .ShouldBe("2 tournaments in this season are in another Series and not listed.");
    }

    [Fact(DisplayName = "OnChanged should fire on each edit")]
    public async Task OnChanged_ShouldFireOnEachEdit()
    {
        // Arrange
        List<SeriesStageRowModel> rows = [new() { TournamentId = "t1" }];
        var cut = Render(rows);

        // Act
        await cut.Find("#stage-0-count").ChangeAsync(new ChangeEventArgs { Value = "5" });
        await cut.Find("#stage-0-tournament").ChangeAsync(new ChangeEventArgs { Value = "t2" });
        await cut.Find("#stage-0-kind").ChangeAsync(new ChangeEventArgs { Value = StageKinds.Survivor });

        // Assert
        _changes.ShouldBe(3);
    }
}