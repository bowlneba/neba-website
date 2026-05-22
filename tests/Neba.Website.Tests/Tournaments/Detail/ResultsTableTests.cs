using Bunit;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Tournaments.Detail;

namespace Neba.Website.Tests.Tournaments.Detail;

[UnitTest]
[Component("Website.Tournaments.Detail.ResultsTable")]
public sealed class ResultsTableTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render a table row for each result")]
    public void Render_ShouldRenderRowPerResult()
    {
        // Arrange
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(place: 1),
            TournamentResultViewModelFactory.Create(place: 2),
            TournamentResultViewModelFactory.Create(place: 3),
        };

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, results));

        // Assert
        cut.FindAll("tbody tr").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should render place number for placed results")]
    public void Render_ShouldRenderPlaceNumber_WhenPlaceIsSet()
    {
        // Arrange
        var result = TournamentResultViewModelFactory.Create(place: 1);

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        // Assert
        cut.Find("tbody tr td:first-child").TextContent.ShouldBe("1");
    }

    [Fact(DisplayName = "Should render em dash for results with no place recorded")]
    public void Render_ShouldRenderDash_WhenPlaceIsNull()
    {
        // Arrange
        var result = TournamentResultViewModelFactory.Create(place: null);

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        // Assert
        cut.Find("tbody tr td:first-child").TextContent.ShouldBe("—");
    }

    [Fact(DisplayName = "Should render bowler name in second column")]
    public void Render_ShouldRenderBowlerName()
    {
        // Arrange
        var result = TournamentResultViewModelFactory.Create(bowlerName: "Jane Smith");

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        // Assert
        cut.Find("tbody tr td:nth-child(2)").TextContent.ShouldBe("Jane Smith");
    }

    [Fact(DisplayName = "Should render prize money formatted as currency")]
    public void Render_ShouldRenderFormattedPrizeMoney()
    {
        // Arrange
        var result = TournamentResultViewModelFactory.Create(prizeMoney: 500m);

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        // Assert
        cut.Find("tbody tr td:nth-child(3)").TextContent.ShouldBe("$500");
    }

    [Fact(DisplayName = "Should render points in fourth column")]
    public void Render_ShouldRenderPoints()
    {
        // Arrange
        var result = TournamentResultViewModelFactory.Create(points: 10);

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        // Assert
        cut.Find("tbody tr td:nth-child(4)").TextContent.ShouldBe("10");
    }

    [Fact(DisplayName = "Should render empty table body when no results")]
    public void Render_ShouldRenderEmptyTableBody_WhenNoResults()
    {
        // Act
        var cut = _ctx.Render<ResultsTable>(p =>
            p.Add(x => x.Results, []));

        // Assert
        cut.FindAll("tbody tr").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render all four column headers")]
    public void Render_ShouldRenderAllColumnHeaders()
    {
        // Act
        var cut = _ctx.Render<ResultsTable>(p =>
            p.Add(x => x.Results, []));

        // Assert
        var headers = cut.FindAll("thead th");
        headers.Count.ShouldBe(4);
        headers[0].TextContent.ShouldBe("Place");
        headers[1].TextContent.ShouldBe("Bowler");
        headers[2].TextContent.ShouldBe("Prize Money");
        headers[3].TextContent.ShouldBe("Points");
    }

    [Fact(DisplayName = "Should combine same-place results into a single row")]
    public void Render_ShouldCombineSamePlaceResults_IntoOneRow()
    {
        // Arrange
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(bowlerName: "Alice", place: 1),
            TournamentResultViewModelFactory.Create(bowlerName: "Bob", place: 1),
            TournamentResultViewModelFactory.Create(bowlerName: "Carol", place: 1),
        };

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        // Assert
        cut.FindAll("tbody tr").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should stack all bowler names in the bowler column for a team row")]
    public void Render_ShouldStackBowlerNames_ForTeamRow()
    {
        // Arrange
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(bowlerName: "Alice", place: 1),
            TournamentResultViewModelFactory.Create(bowlerName: "Bob", place: 1),
        };

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        // Assert
        var nameCell = cut.Find("tbody tr td:nth-child(2)");
        nameCell.QuerySelectorAll("div").Select(d => d.TextContent)
            .ShouldBe(["Alice", "Bob"], ignoreOrder: false);
    }

    [Fact(DisplayName = "Should sum prize money for combined team rows")]
    public void Render_ShouldSumPrizeMoney_ForTeamRow()
    {
        // Arrange
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(place: 1, prizeMoney: 950m),
            TournamentResultViewModelFactory.Create(place: 1, prizeMoney: 950m),
            TournamentResultViewModelFactory.Create(place: 1, prizeMoney: 950m),
        };

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        // Assert
        cut.Find("tbody tr td:nth-child(3)").TextContent.ShouldBe("$2,850");
    }

    [Fact(DisplayName = "Should keep null-place results as separate rows")]
    public void Render_ShouldKeepNullPlaceResults_AsSeparateRows()
    {
        // Arrange
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(bowlerName: "Alice", place: null),
            TournamentResultViewModelFactory.Create(bowlerName: "Bob", place: null),
        };

        // Act
        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        // Assert
        cut.FindAll("tbody tr").Count.ShouldBe(2);
    }
}
