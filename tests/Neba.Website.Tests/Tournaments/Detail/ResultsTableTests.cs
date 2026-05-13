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
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(place: 1),
            TournamentResultViewModelFactory.Create(place: 2),
            TournamentResultViewModelFactory.Create(place: 3),
        };

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, results));

        cut.FindAll("tbody tr").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should render place number for placed results")]
    public void Render_ShouldRenderPlaceNumber_WhenPlaceIsSet()
    {
        var result = TournamentResultViewModelFactory.Create(place: 1);

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        cut.Find("tbody tr td:first-child").TextContent.ShouldBe("1");
    }

    [Fact(DisplayName = "Should render em dash for results with no place recorded")]
    public void Render_ShouldRenderDash_WhenPlaceIsNull()
    {
        var result = TournamentResultViewModelFactory.Create(place: null);

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        cut.Find("tbody tr td:first-child").TextContent.ShouldBe("—");
    }

    [Fact(DisplayName = "Should render bowler name in second column")]
    public void Render_ShouldRenderBowlerName()
    {
        var result = TournamentResultViewModelFactory.Create(bowlerName: "Jane Smith");

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        cut.Find("tbody tr td:nth-child(2)").TextContent.ShouldBe("Jane Smith");
    }

    [Fact(DisplayName = "Should render prize money formatted as currency")]
    public void Render_ShouldRenderFormattedPrizeMoney()
    {
        var result = TournamentResultViewModelFactory.Create(prizeMoney: 500m);

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        cut.Find("tbody tr td:nth-child(3)").TextContent.ShouldBe("$500");
    }

    [Fact(DisplayName = "Should render points in fourth column")]
    public void Render_ShouldRenderPoints()
    {
        var result = TournamentResultViewModelFactory.Create(points: 10);

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)[result]));

        cut.Find("tbody tr td:nth-child(4)").TextContent.ShouldBe("10");
    }

    [Fact(DisplayName = "Should render empty table body when no results")]
    public void Render_ShouldRenderEmptyTableBody_WhenNoResults()
    {
        var cut = _ctx.Render<ResultsTable>(p =>
            p.Add(x => x.Results, []));

        cut.FindAll("tbody tr").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render all four column headers")]
    public void Render_ShouldRenderAllColumnHeaders()
    {
        var cut = _ctx.Render<ResultsTable>(p =>
            p.Add(x => x.Results, []));

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
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(bowlerName: "Alice", place: 1),
            TournamentResultViewModelFactory.Create(bowlerName: "Bob", place: 1),
            TournamentResultViewModelFactory.Create(bowlerName: "Carol", place: 1),
        };

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        cut.FindAll("tbody tr").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should stack all bowler names in the bowler column for a team row")]
    public void Render_ShouldStackBowlerNames_ForTeamRow()
    {
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(bowlerName: "Alice", place: 1),
            TournamentResultViewModelFactory.Create(bowlerName: "Bob", place: 1),
        };

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        var nameCell = cut.Find("tbody tr td:nth-child(2)");
        nameCell.QuerySelectorAll("div").Select(d => d.TextContent)
            .ShouldBe(["Alice", "Bob"], ignoreOrder: false);
    }

    [Fact(DisplayName = "Should sum prize money for combined team rows")]
    public void Render_ShouldSumPrizeMoney_ForTeamRow()
    {
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(place: 1, prizeMoney: 950m),
            TournamentResultViewModelFactory.Create(place: 1, prizeMoney: 950m),
            TournamentResultViewModelFactory.Create(place: 1, prizeMoney: 950m),
        };

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        cut.Find("tbody tr td:nth-child(3)").TextContent.ShouldBe("$2,850");
    }

    [Fact(DisplayName = "Should keep null-place results as separate rows")]
    public void Render_ShouldKeepNullPlaceResults_AsSeparateRows()
    {
        var results = new[]
        {
            TournamentResultViewModelFactory.Create(bowlerName: "Alice", place: null),
            TournamentResultViewModelFactory.Create(bowlerName: "Bob", place: null),
        };

        var cut = _ctx.Render<ResultsTable>(p => p.Add(x => x.Results, (IEnumerable<TournamentResultViewModel>)results));

        cut.FindAll("tbody tr").Count.ShouldBe(2);
    }
}