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