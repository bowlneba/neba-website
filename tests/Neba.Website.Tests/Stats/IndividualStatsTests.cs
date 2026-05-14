using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;
using Neba.Website.Server.Stats;

using IndividualStatsPage = Neba.Website.Server.Stats.IndividualStats;

namespace Neba.Website.Tests.Stats;

[UnitTest]
[Component("Website.Stats.IndividualStats")]
public sealed class IndividualStatsTests : IDisposable
{
    private const string BowlerId = "01JX0000011111111111111111";
    private const int Season1Year = 2024;
    private const int Season2Year = 2023;

    private readonly BunitContext _ctx;
    private readonly FakeStatsApiService _statsApi;

    public IndividualStatsTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _statsApi = new FakeStatsApiService();
        _ctx.Services.AddSingleton<IStatsApiService>(_statsApi);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show skeleton loaders while initial request is pending")]
    public void Render_ShouldShowSkeletonLoaders_WhenRequestIsPending()
    {
        // Arrange
        _statsApi.EnqueueIndividualTask(new TaskCompletionSource<IndividualStatsPageViewModel?>().Task);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.FindAll(".neba-skeleton").Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should show not-found state and skip API call when BowlerId is empty")]
    public void Render_ShouldShowNotFound_WhenBowlerIdIsEmpty()
    {
        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, " "));

        // Assert
        cut.Markup.ShouldContain("Bowler not found");
        _statsApi.TotalIndividualCalls.ShouldBe(0);
    }

    [Fact(DisplayName = "Should show not-found state when API returns null")]
    public void Render_ShouldShowNotFound_WhenApiReturnsNull()
    {
        // Arrange
        _statsApi.EnqueueIndividualResult(null);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Markup.ShouldContain("Bowler not found");
    }

    [Fact(DisplayName = "Should render bowler name and selected season when model loads")]
    public void Render_ShouldShowBowlerNameAndSeason_WhenModelLoads()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(
            bowlerName: "Jane Doe",
            selectedSeason: "2024-2025");
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Find(".indiv-bowler-name").TextContent.ShouldBe("Jane Doe");
        cut.Find(".indiv-season-label").TextContent.ShouldBe("2024-2025");
    }

    [Fact(DisplayName = "Should render season selector above bowler heading when model loads")]
    public void Render_ShouldPlaceSeasonSelectorAboveBowlerHeading_WhenModelLoads()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(
            bowlerName: "Jane Doe",
            selectedSeason: "2020-2021 Season",
            availableSeasons: new Dictionary<int, string>
            {
                [Season1Year] = "2024-2025",
                [Season2Year] = "2020-2021 Season",
            });
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        var heroBody = cut.Find(".indiv-hero-body");
        heroBody.Children[0].ClassList.Contains("stats-season-selector").ShouldBeTrue();
        heroBody.Children[1].ClassList.Contains("indiv-hero-title").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should render stat strip with points, average, games, finals, entries, tournaments, and winnings label")]
    public void Render_ShouldShowStatStrip_WhenModelLoads()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(
            points: 75,
            average: 213.50m,
            games: 30,
            finals: 4,
            entries: 8,
            tournaments: 6,
            winnings: 1250m);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        var markup = cut.Markup;
        markup.ShouldContain("75");
        markup.ShouldContain("213.50");
        markup.ShouldContain("30");
        markup.ShouldContain("$ Won");
    }

    [Fact(DisplayName = "Should hide match play record stat card when wins and losses are both zero")]
    public void Render_ShouldHideMatchPlayRecord_WhenNoMatchPlayGames()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(matchPlayWins: 0, matchPlayLosses: 0);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Markup.ShouldNotContain("MP Record");
    }

    [Fact(DisplayName = "Should show match play record stat card with win-loss values when match play games exist")]
    public void Render_ShouldShowMatchPlayRecord_WhenMatchPlayGamesExist()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(matchPlayWins: 7, matchPlayLosses: 3);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Markup.ShouldContain("MP Record");
        cut.Markup.ShouldContain("7");
        cut.Markup.ShouldContain("3");
    }

    [Fact(DisplayName = "Should show match play average detail row when value is present")]
    public void Render_ShouldShowMatchPlayAverageDetail_WhenValueIsPresent()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(matchPlayAverage: 205.50m);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert — "Match Play Average" (detail label) is distinct from "Match Play Avg" (rank card title)
        cut.Markup.ShouldContain("Match Play Average");
        cut.Markup.ShouldContain("205.50");
    }

    [Fact(DisplayName = "Should hide match play average detail row when value is absent")]
    public void Render_ShouldHideMatchPlayAverageDetail_WhenValueIsAbsent()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(matchPlayAverage: null);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert — "Match Play Average" (label) only appears in the detail row, not in rank card titles
        cut.Markup.ShouldNotContain("Match Play Average");
    }

    [Fact(DisplayName = "Should show win percentage detail row when match play games exist")]
    public void Render_ShouldShowWinPercentageDetail_WhenMatchPlayGamesExist()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(matchPlayWins: 8, matchPlayLosses: 2);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert — WinPercentage = 8/10 * 100 = 80.0
        cut.Markup.ShouldContain("Win Percentage");
        cut.Markup.ShouldContain("80.0%");
    }

    [Fact(DisplayName = "Should hide win percentage detail row when no match play games")]
    public void Render_ShouldHideWinPercentageDetail_WhenNoMatchPlayGames()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(matchPlayWins: 0, matchPlayLosses: 0);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Markup.ShouldNotContain("Win Percentage");
    }

    [Fact(DisplayName = "Should show positive field average with plus prefix and success color class")]
    public void Render_ShouldShowPositiveFieldAverage_WithPlusPrefixAndSuccessClass()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(fieldAverage: 8.25m);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        var fieldVal = cut.Find(".indiv-vs-field-value");
        fieldVal.TextContent.ShouldBe("+8.25");
        (fieldVal.ClassName ?? string.Empty).ShouldContain("neba-success");
    }

    [Fact(DisplayName = "Should show negative field average with its sign and gray color class")]
    public void Render_ShouldShowNegativeFieldAverage_WithNegativeSignAndGrayClass()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(fieldAverage: -4.10m);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        var fieldVal = cut.Find(".indiv-vs-field-value");
        fieldVal.TextContent.ShouldBe("-4.10");
        (fieldVal.ClassName ?? string.Empty).ShouldContain("text-gray-400");
    }

    [Fact(DisplayName = "Should show one progression card per BOY progression entry")]
    public void Render_ShouldShowOneCardPerBoyProgression()
    {
        // Arrange
        var progressions = new[]
        {
            new IndividualBoyProgressionViewModel { RaceLabel = "Bowler of the Year", BowlerSeries = PointsRaceSeriesViewModelFactory.Create(), LeaderSeries = null },
            new IndividualBoyProgressionViewModel { RaceLabel = "Senior", BowlerSeries = PointsRaceSeriesViewModelFactory.Create(), LeaderSeries = null },
        };
        var model = IndividualStatsPageViewModelFactory.Create(boyProgressions: progressions);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.FindAll(".indiv-points-race-card").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Should not show any progression cards when BoyProgressions is empty")]
    public void Render_ShouldHidePointsRaceCards_WhenBoyProgressionsEmpty()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(boyProgressions: []);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.FindAll(".indiv-points-race-card").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should display the race label in each progression card header")]
    public void Render_ShouldShowRaceLabelInCardHeader()
    {
        // Arrange
        var progressions = new[]
        {
            new IndividualBoyProgressionViewModel { RaceLabel = "Bowler of the Year", BowlerSeries = PointsRaceSeriesViewModelFactory.Create(), LeaderSeries = null },
        };
        var model = IndividualStatsPageViewModelFactory.Create(boyProgressions: progressions);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Find(".indiv-points-race-card-header h2").TextContent.ShouldContain("Bowler of the Year");
    }

    [Fact(DisplayName = "Should use Season query parameter to set the active season button when param is valid")]
    public void Render_ShouldSetActiveSeasonFromQueryParam_WhenSeasonParamIsValid()
    {
        var model = IndividualStatsPageViewModelFactory.Create(
            selectedSeason: "2024-2025",
            availableSeasons: new Dictionary<int, string>
            {
                [Season1Year] = "2024-2025",
                [Season2Year] = "2023-2024",
            });
        _statsApi.EnqueueIndividualResult(model);
        _ctx.Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/stats/{BowlerId}?season={Season2Year}");

        var cut = _ctx.Render<IndividualStatsPage>(p => p.Add(x => x.BowlerId, BowlerId));

        cut.FindAll(".stats-season-btn.active").Single().TextContent.Trim().ShouldContain("2023-2024");
    }

    [Fact(DisplayName = "Should fall back to first available season when selected season name has no match")]
    public void Render_ShouldFallbackToFirstSeason_WhenSelectedSeasonNameHasNoMatch()
    {
        var model = IndividualStatsPageViewModelFactory.Create(
            selectedSeason: "No Match",
            availableSeasons: new Dictionary<int, string>
            {
                [Season1Year] = "2024-2025",
                [Season2Year] = "2023-2024",
            });
        _statsApi.EnqueueIndividualResult(model);

        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        cut.FindAll(".stats-season-btn.active").Single().TextContent.Trim().ShouldContain("2024-2025");
    }

    [Fact(DisplayName = "Should highlight first season as active and show additional seasons as inactive on initial load")]
    public void Render_ShouldSetFirstSeasonAsActive_OnInitialLoad()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(
            selectedSeason: "2024-2025",
            availableSeasons: new Dictionary<int, string>
            {
                [Season1Year] = "2024-2025",
                [Season2Year] = "2023-2024",
            });
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        var buttons = cut.FindAll(".stats-season-btn");
        (buttons[0].ClassName ?? string.Empty).ShouldContain("active");
        (buttons[1].ClassName ?? string.Empty).ShouldNotContain("active");
    }

    [Fact(DisplayName = "Should call API with the selected season year, update the active button, and display new data when a season is changed")]
    public async Task SeasonSelection_ShouldLoadNewSeason_WhenSeasonButtonClicked()
    {
        // Arrange
        var seasons = new Dictionary<int, string>
        {
            [Season1Year] = "2024-2025",
            [Season2Year] = "2023-2024",
        };

        _statsApi.EnqueueIndividualResult(IndividualStatsPageViewModelFactory.Create(
            bowlerName: "Jane Doe",
            selectedSeason: "2024-2025",
            availableSeasons: seasons));

        _statsApi.EnqueueIndividualResult(IndividualStatsPageViewModelFactory.Create(
            bowlerName: "Jane Doe",
            selectedSeason: "2023-2024",
            availableSeasons: seasons));

        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Act
        await cut.FindAll(".stats-season-btn")[1].ClickAsync(new());

        // Assert
        cut.Find(".indiv-season-label").TextContent.ShouldBe("2023-2024");
        (cut.FindAll(".stats-season-btn.active").Single().TextContent ?? string.Empty).ShouldContain("2023-2024");
        _statsApi.IndividualStatsCalls.ShouldBe(
        [
            (BowlerId, null),
            (BowlerId, Season2Year),
        ]);
    }

    [Fact(DisplayName = "Should show ranked cards with rank numbers and unranked cards with em dash for null ranks")]
    public void Render_ShouldShowRankedAndUnrankedCards_ByRankNullability()
    {
        // Arrange
        var model = IndividualStatsPageViewModelFactory.Create(
            bowlerOfTheYearRank: 3,
            seniorOfTheYearRank: null,
            superSeniorOfTheYearRank: null,
            womanOfTheYearRank: null,
            rookieOfTheYearRank: null,
            youthOfTheYearRank: null);
        _statsApi.EnqueueIndividualResult(model);

        // Act
        var cut = _ctx.Render<IndividualStatsPage>(p => p
            .Add(x => x.BowlerId, BowlerId));

        // Assert
        cut.Markup.ShouldContain("#3");
        cut.FindAll(".indiv-rank-card--blue-500").Count.ShouldBe(1);
        cut.FindAll(".indiv-rank-card--unranked").Count.ShouldBeGreaterThan(0);
    }

    private sealed class FakeStatsApiService : IStatsApiService
    {
        private readonly Queue<Task<IndividualStatsPageViewModel?>> _results = [];

        public int TotalIndividualCalls { get; private set; }
        public List<(string BowlerId, int? Year)> IndividualStatsCalls { get; } = [];

        public void EnqueueIndividualResult(IndividualStatsPageViewModel? model)
            => _results.Enqueue(Task.FromResult(model));

        public void EnqueueIndividualTask(Task<IndividualStatsPageViewModel?> task)
            => _results.Enqueue(task);

        public Task<StatsPageViewModel> GetStatsAsync(int? year = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<IndividualStatsPageViewModel?> GetIndividualStatsAsync(string bowlerId, int? year = null, CancellationToken ct = default)
        {
            TotalIndividualCalls++;
            IndividualStatsCalls.Add((bowlerId, year));

            return _results.Count == 0
                ? throw new InvalidOperationException("No queued individual stats result.")
                : _results.Dequeue();
        }
    }
}