using Bunit;

using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Bowlers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Champions;
using Neba.Website.Server.Clock;
using Neba.Website.Server.History.Champions;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

using ChampionsPage = Neba.Website.Server.History.Champions.Champions;

namespace Neba.Website.Tests.History.Champions;

[UnitTest]
[Component("Website.History.Champions.Champions")]
public sealed class ChampionsPageTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IBowlersApi> _mockBowlersApi;

    public ChampionsPageTests()
    {
        _mockBowlersApi = new Mock<IBowlersApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(_mockBowlersApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should show skeleton in hero stats during data load")]
    public void Render_ShouldShowHeroSkeleton_WhenLoading()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>();
        RegisterService(new FakeChampionsService(pending.Task));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — skeleton boxes are visible inside the hero; raw stat numbers are not
        cut.Markup.ShouldContain("hero-stat-skeleton__num");
        cut.Markup.ShouldNotContain("hero-stat__num");
    }

    [Fact(DisplayName = "Should show champions content skeleton during data load")]
    public void Render_ShouldShowChampionsSkeleton_WhenLoading()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>();
        RegisterService(new FakeChampionsService(pending.Task));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert
        cut.Markup.ShouldContain("champions-skeleton");
        cut.Markup.ShouldNotContain("tier-section");
    }

    [Fact(DisplayName = "Should disable toolbar buttons during data load")]
    public void Render_ShouldDisableToolbarButtons_WhenLoading()
    {
        // Arrange
        var pending = new TaskCompletionSource<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>();
        RegisterService(new FakeChampionsService(pending.Task));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — every toolbar button carries the disabled attribute
        cut.FindAll("button[disabled]").Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should show -- placeholders in hero stats when API fails")]
    public void Render_ShouldShowDashPlaceholders_WhenApiError()
    {
        // Arrange
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            Error.Failure("Champions.Unavailable", "Failed to load champions."));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — at least one stat shows the -- placeholder
        cut.Markup.ShouldContain("--");
    }

    [Fact(DisplayName = "Should show all four -- stats when API fails")]
    public void Render_ShouldShowAllFourDashStats_WhenApiError()
    {
        // Arrange
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            Error.Failure("Champions.Unavailable", "Failed to load champions."));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — all four hero stats show -- (including First Year which is normally hidden when data is absent)
        var statNums = cut.FindAll(".hero-stat__num");
        statNums.Count.ShouldBe(4);
        statNums.ShouldAllBe(n => n.TextContent.Trim() == "--");
    }

    [Fact(DisplayName = "Should show error alert when API fails")]
    public void Render_ShouldShowErrorAlert_WhenApiError()
    {
        // Arrange
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            Error.Failure("Champions.Unavailable", "Failed to load champions."));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert
        cut.Markup.ShouldContain("neba-alert");
        cut.Markup.ShouldContain("Failed to load champions.");
    }

    [Fact(DisplayName = "Should disable toolbar buttons when API fails")]
    public void Render_ShouldDisableToolbarButtons_WhenApiError()
    {
        // Arrange
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            Error.Failure("Champions.Unavailable", "Failed to load champions."));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert
        cut.FindAll("button[disabled]").Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should show actual hero stats when data loads successfully")]
    public void Render_ShouldShowActualStats_WhenLoaded()
    {
        // Arrange
        var summary = BowlerTitleSummaryViewModelFactory.Create(titleCount: 3, hallOfFame: true);
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [summary];
        IReadOnlyCollection<TitlesByYearViewModel> years = [];

        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — skeleton is gone and real stats are rendered
        cut.Markup.ShouldNotContain("hero-stat-skeleton__num");
        cut.Markup.ShouldNotContain("champions-skeleton");
        cut.Markup.ShouldContain("hero-stat__num");
    }

    [Fact(DisplayName = "Should enable toolbar buttons when data loads successfully")]
    public void Render_ShouldEnableToolbarButtons_WhenLoaded()
    {
        // Arrange
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [];
        IReadOnlyCollection<TitlesByYearViewModel> years = [];

        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — no disabled buttons once data is loaded
        cut.FindAll("button[disabled]").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "Should show FirstYear stat when years data is available")]
    public void Render_ShouldShowFirstYearStat_WhenYearsDataIsLoaded()
    {
        // Arrange
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [];
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2018),
        ];
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — 4 stats including the first year
        var statNums = cut.FindAll(".hero-stat__num");
        statNums.Count.ShouldBe(4);
        statNums[^1].TextContent.Trim().ShouldBe("2018");
    }

    [Fact(DisplayName = "Should not show FirstYear stat when no years data exists")]
    public void Render_ShouldNotShowFirstYearStat_WhenNoYearsData()
    {
        // Arrange
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [];
        IReadOnlyCollection<TitlesByYearViewModel> years = [];
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));

        // Act
        var cut = _ctx.Render<ChampionsPage>();

        // Assert — only 3 stats; First Year stat is omitted when years is empty
        var statNums = cut.FindAll(".hero-stat__num");
        statNums.Count.ShouldBe(3);
    }

    [Fact(DisplayName = "ExpandAll should delegate to TitleCountView when Titles view is active")]
    public async Task ExpandAll_ShouldDelegateToTitleCountView_WhenTitlesViewIsActive()
    {
        // Arrange — load data so views are rendered
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [];
        IReadOnlyCollection<TitlesByYearViewModel> years = [];
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));
        var cut = _ctx.Render<ChampionsPage>();

        // Act — Titles view is active by default; click Expand All
        var expandButton = cut.Find(".toolbar-actions button:first-child");
        await expandButton.ClickAsync();

        // Assert — no exception; component remains in a valid state
        cut.Markup.ShouldContain("view is-active");
    }

    [Fact(DisplayName = "ExpandAll should delegate to YearView when Year view is active")]
    public async Task ExpandAll_ShouldDelegateToYearView_WhenYearViewIsActive()
    {
        // Arrange
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [];
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2023),
            TitlesByYearViewModelFactory.Create(year: 2022),
            TitlesByYearViewModelFactory.Create(year: 2021),
            TitlesByYearViewModelFactory.Create(year: 2020),
        ];
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));
        var cut = _ctx.Render<ChampionsPage>();

        // Switch to Year view — By Year button is the second segmented button
        await cut.FindAll(".segmented__btn")[1].ClickAsync();

        // Verify some sections are collapsed
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBeGreaterThan(0);

        // Act — Expand All is the first toolbar-actions button
        await cut.Find(".toolbar-actions button:first-child").ClickAsync();

        // Assert
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(0);
    }

    [Fact(DisplayName = "CollapseAll should delegate to YearView when Year view is active")]
    public async Task CollapseAll_ShouldDelegateToYearView_WhenYearViewIsActive()
    {
        // Arrange
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [];
        IReadOnlyCollection<TitlesByYearViewModel> years =
        [
            TitlesByYearViewModelFactory.Create(year: 2024),
            TitlesByYearViewModelFactory.Create(year: 2023),
        ];
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));
        var cut = _ctx.Render<ChampionsPage>();

        // Switch to Year view
        await cut.FindAll(".segmented__btn")[1].ClickAsync();

        // All 2 years expanded initially
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(0);

        // Act — Collapse All is the second toolbar-actions button
        await cut.Find(".toolbar-actions button:last-child").ClickAsync();

        // Assert
        cut.FindAll(".year-section.is-collapsed").Count.ShouldBe(2);
    }

    [Fact(DisplayName = "CollapseAll should delegate to TitleCountView when Titles view is active")]
    public async Task CollapseAll_ShouldDelegateToTitleCountView_WhenTitlesViewIsActive()
    {
        // Arrange
        IReadOnlyCollection<BowlerTitleSummaryViewModel> summaries = [];
        IReadOnlyCollection<TitlesByYearViewModel> years = [];
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            (summaries, years));
        RegisterService(new FakeChampionsService(result));
        var cut = _ctx.Render<ChampionsPage>();

        // No sections collapsed initially
        cut.FindAll(".tier-section.is-collapsed").Count.ShouldBe(0);

        // Act — Titles view is active by default; Collapse All is the second toolbar-actions button
        await cut.Find(".toolbar-actions button:last-child").ClickAsync();

        // Assert — all 3 tiers collapsed
        cut.FindAll(".tier-section.is-collapsed").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should dismiss error alert when OnDismiss is invoked")]
    public async Task Render_ShouldDismissAlert_WhenOnDismissIsInvoked()
    {
        // Arrange
        var result = Task.FromResult<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel>, IReadOnlyCollection<TitlesByYearViewModel>)>>(
            Error.Failure("Champions.Unavailable", "Failed to load champions."));
        RegisterService(new FakeChampionsService(result));
        var cut = _ctx.Render<ChampionsPage>();
        cut.Markup.ShouldContain("neba-alert");

        // Act — click the dismiss button (aria-label set by NebaAlert)
        await cut.Find("button[aria-label='Dismiss alert']").ClickAsync();

        // Assert
        cut.Markup.ShouldNotContain("neba-alert");
    }

    private void RegisterService(FakeChampionsService service)
        => _ctx.Services.AddSingleton<ITournamentApiService>(service);

    private sealed class FakeChampionsService(
        Task<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel> Summaries, IReadOnlyCollection<TitlesByYearViewModel> Years)>> task)
        : ITournamentApiService
    {
        Task<ErrorOr<(IReadOnlyCollection<BowlerTitleSummaryViewModel> Summaries, IReadOnlyCollection<TitlesByYearViewModel> Years)>>
#pragma warning disable VSTHRD003
            ITournamentApiService.GetChampionsDataAsync(CancellationToken ct) => task;
#pragma warning restore VSTHRD003

        Task<ErrorOr<List<SeasonTournamentViewModel>>>
            ITournamentApiService.GetTournamentsForSeasonAsync(SeasonViewModel season, CancellationToken ct)
            => Task.FromResult<ErrorOr<List<SeasonTournamentViewModel>>>(new List<SeasonTournamentViewModel>());

        Task<ErrorOr<List<SeasonViewModel>>>
            ITournamentApiService.GetSeasonsAsync(CancellationToken ct)
            => Task.FromResult<ErrorOr<List<SeasonViewModel>>>(new List<SeasonViewModel>());
    }
}