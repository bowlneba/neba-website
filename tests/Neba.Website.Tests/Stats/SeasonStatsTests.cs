using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Neba.TestFactory.Attributes;
using Neba.TestFactory.Stats;
using Neba.Website.Server.Stats;

using SeasonStatsPage = Neba.Website.Server.Stats.SeasonStats;

namespace Neba.Website.Tests.Stats;

[UnitTest]
[Component("Website.Stats.SeasonStats")]
public sealed class SeasonStatsTests : IDisposable
{
    private const int s_firstSeasonYear = 2024;
    private const int s_secondSeasonYear = 2023;
    private const string s_searchBowlerId = "01JX0000033333333333333333";

    private readonly BunitContext _ctx;
    private readonly FakeStatsApiService _statsApi;

    public SeasonStatsTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _statsApi = new FakeStatsApiService();
        _ctx.Services.AddSingleton<IStatsApiService>(_statsApi);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render season stats title and first season data after loading")]
    public void Render_ShouldShowStatsPage_WhenInitialLoadCompletes()
    {
        // Arrange
        var model = CreateStatsModel(
            firstRowName: "Alpha Bowler",
            firstSeasonName: "2024-2025",
            secondSeasonName: "2023-2024");

        _statsApi.EnqueueResult(model);

        // Act
        var cut = _ctx.Render<SeasonStatsPage>();

        // Assert
        cut.Markup.ShouldContain("Season Statistics");
        cut.Markup.ShouldContain("Alpha Bowler");
        cut.Markup.ShouldContain("2024-2025");
    }

    [Fact(DisplayName = "Should show loading skeleton while initial stats request is pending")]
    public void Render_ShouldShowSkeletonLoader_WhenInitialRequestIsPending()
    {
        // Arrange
        _statsApi.EnqueueTask(new TaskCompletionSource<StatsPageViewModel>().Task);

        // Act
        var cut = _ctx.Render<SeasonStatsPage>();

        // Assert
        cut.FindAll(".neba-skeleton").Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should show load error when API returns no available seasons")]
    public void Render_ShouldShowLoadError_WhenAvailableSeasonsIsEmpty()
    {
        // Arrange
        var model = StatsPageViewModelFactory.Create(
            availableSeasons: new Dictionary<int, string>(),
            bowlerOfTheYear:
            [
                BowlerOfTheYearStandingRowViewModelFactory.Create(bowlerName: "Alpha Bowler"),
            ]);

        _statsApi.EnqueueResult(model);

        // Act
        var cut = _ctx.Render<SeasonStatsPage>();

        // Assert
        cut.Markup.ShouldContain("Unable to load season stats right now.");
    }

    [Fact(DisplayName = "Should switch active tab panel when a different tab is clicked")]
    public async Task Tabs_ShouldSwitchVisiblePanel_WhenTabClicked()
    {
        // Arrange
        _statsApi.EnqueueResult(CreateStatsModel(firstRowName: "Alpha Bowler"));

        var cut = _ctx.Render<SeasonStatsPage>();

        // Act
        await cut.Find("#tab-match-play").ClickAsync(new());

        // Assert
        (cut.Find("#panel-award-standings").ClassName ?? string.Empty).ShouldContain("tab-hidden");
        (cut.Find("#panel-match-play").ClassName ?? string.Empty).ShouldNotContain("tab-hidden");
    }

    [Fact(DisplayName = "Should load and display second season stats when season button is clicked")]
    public async Task SeasonSelection_ShouldLoadSelectedSeason_WhenSeasonButtonClicked()
    {
        // Arrange
        var firstSeasonModel = CreateStatsModel(
            firstRowName: "Alpha Bowler",
            firstSeasonName: "2024-2025",
            secondSeasonName: "2023-2024");
        var secondSeasonModel = CreateStatsModel(
            firstRowName: "Bravo Bowler",
            firstSeasonName: "2024-2025",
            secondSeasonName: "2023-2024");

        _statsApi.EnqueueResult(firstSeasonModel);
        _statsApi.EnqueueResult(secondSeasonModel);

        var cut = _ctx.Render<SeasonStatsPage>();

        // Act
        await cut.FindAll(".stats-season-btn")[1].ClickAsync(new());

        // Assert
        var markup = cut.Markup ?? string.Empty;
        markup.ShouldContain("Bravo Bowler");
        markup.ShouldNotContain("Alpha Bowler");
        (cut.FindAll(".stats-season-btn.active").Single().TextContent ?? string.Empty).ShouldContain("2023-2024");
        _statsApi.RequestedSeasonYears.ShouldBe([null, s_secondSeasonYear]);
    }

    [Fact(DisplayName = "Should navigate to selected bowler page when search value matches a bowler")]
    public async Task Search_ShouldNavigateToBowlerPage_WhenExactMatchIsSelected()
    {
        // Arrange
        _statsApi.EnqueueResult(CreateStatsModel(firstRowName: "Alpha Bowler"));

        var cut = _ctx.Render<SeasonStatsPage>();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        const string targetName = "Search Target";

        // Act
        await cut.Find(".stats-search-input")
            .ChangeAsync(new ChangeEventArgs { Value = targetName });

        // Assert
        nav.Uri.ShouldEndWith($"/stats/{s_searchBowlerId}?season={s_firstSeasonYear}");
    }

    [Fact(DisplayName = "Should show match play record minimum games qualifier based on minimum tournaments")]
    public async Task Render_ShouldShowMatchPlayRecordMinimumGamesQualifier_BasedOnMinimumTournaments()
    {
        // Arrange
        var model = CreateStatsModel(firstRowName: "Alpha Bowler") with
        {
            MinimumNumberOfTournaments = 2m,
        };

        _statsApi.EnqueueResult(model);

        var cut = _ctx.Render<SeasonStatsPage>();

        // Act
        await cut.Find("#tab-match-play").ClickAsync(new());

        // Assert
        cut.Markup.ShouldContain("Match Play Record");
        cut.Markup.ShouldContain("min 4 games");
    }

    private static StatsPageViewModel CreateStatsModel(
        string firstRowName,
        string firstSeasonName = "2024-2025",
        string secondSeasonName = "2023-2024")
    {
        return StatsPageViewModelFactory.Create(
            availableSeasons: new Dictionary<int, string>
            {
                [s_firstSeasonYear] = firstSeasonName,
                [s_secondSeasonYear] = secondSeasonName,
            },
            bowlerSearchList: new Dictionary<string, string>
            {
                [s_searchBowlerId] = "Search Target",
                [Ulid.NewUlid().ToString()] = "Another Bowler",
            },
            bowlerOfTheYear:
            [
                BowlerOfTheYearStandingRowViewModelFactory.Create(
                    bowlerName: firstRowName),
            ],
            seniorOfTheYear:
            [
                BowlerOfTheYearStandingRowViewModelFactory.Create(bowlerName: firstRowName),
            ],
            superSeniorOfTheYear:
            [
                BowlerOfTheYearStandingRowViewModelFactory.Create(bowlerName: firstRowName),
            ],
            womanOfTheYear:
            [
                BowlerOfTheYearStandingRowViewModelFactory.Create(bowlerName: firstRowName),
            ],
            rookieOfTheYear:
            [
                BowlerOfTheYearStandingRowViewModelFactory.Create(bowlerName: firstRowName),
            ],
            youthOfTheYear:
            [
                BowlerOfTheYearStandingRowViewModelFactory.Create(bowlerName: firstRowName),
            ],
            bowlerOfTheYearPointsRace: PointsRaceSeriesViewModelFactory.Bogus(3, seed: 1103));
    }

    private sealed class FakeStatsApiService
        : IStatsApiService
    {
        private readonly Queue<Task<StatsPageViewModel>> _results = [];

        public List<int?> RequestedSeasonYears { get; } = [];

        public void EnqueueResult(StatsPageViewModel model)
        {
            _results.Enqueue(Task.FromResult(model));
        }

        public void EnqueueTask(Task<StatsPageViewModel> modelTask)
        {
            _results.Enqueue(modelTask);
        }

        public Task<IndividualStatsPageViewModel?> GetIndividualStatsAsync(string bowlerId, int? year = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<StatsPageViewModel> GetStatsAsync(int? year = null, CancellationToken ct = default)
        {
            RequestedSeasonYears.Add(year);

            if (_results.Count == 0)
            {
                throw new InvalidOperationException("No queued stats result available for GetStatsAsync.");
            }

            return _results.Dequeue();
        }
    }
}