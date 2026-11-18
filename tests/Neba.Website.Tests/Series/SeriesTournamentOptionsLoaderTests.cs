using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;
using Neba.Website.Tests.TestSupport;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesTournamentOptionsLoaderTests : IDisposable
{
    private const string SeasonId = "season-1";

    private readonly Mock<ISeasonsApi> _seasonsApi = new(MockBehavior.Strict);
    private readonly Mock<ISeriesApi> _seriesApi = new(MockBehavior.Strict);
    private readonly List<IDisposable> _disposables = [];
    private readonly SeriesTournamentOptionsLoader _loader;

    public SeriesTournamentOptionsLoaderTests()
    {
        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var executor = new ApiExecutor(stopwatch.Object, new StubNavigationManager(), NullLogger<ApiExecutor>.Instance);
        _loader = new SeriesTournamentOptionsLoader(executor, _seasonsApi.Object, _seriesApi.Object);
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    private T Own<T>(T disposable)
        where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    private static SeasonTournamentResponse Tournament(string id, DateOnly start, string status = "Scheduled", int teamSize = 1)
        => new()
        {
            Id = id,
            Name = "Tournament " + id,
            StartDate = start,
            EndDate = start,
            TournamentType = "Singles",
            Status = status,
            TitleEligible = true,
            TeamSize = teamSize,
        };

    private void SetupTournaments(params SeasonTournamentResponse[] tournaments)
    {
        var response = Own(new StubApiResponse<CollectionResponse<SeasonTournamentResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<SeasonTournamentResponse> { Items = tournaments },
        });
        _seasonsApi
            .Setup(x => x.ListTournamentsInSeasonAsync(SeasonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupSeriesList(params SeriesSummaryResponse[] series)
    {
        var response = Own(new StubApiResponse<CollectionResponse<SeriesSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<SeriesSummaryResponse> { Items = series },
        });
        _seriesApi.Setup(x => x.ListSeriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    [Fact(DisplayName = "LoadAsync should leave out tournaments held by another Series and cancelled ones, sorted by date")]
    public async Task LoadAsync_ShouldExcludeHeldAndCancelled_AndSortByDate()
    {
        // Arrange
        SetupTournaments(
            Tournament("late", new DateOnly(2025, 8, 1)),
            Tournament("held", new DateOnly(2025, 2, 1)),
            Tournament("cancelled", new DateOnly(2025, 3, 1), status: "Cancelled"),
            Tournament("early", new DateOnly(2025, 1, 1), teamSize: 3));
        SetupSeriesList(SeriesSummaryResponseFactory.Create(id: "other", seasonId: SeasonId, stageTournamentIds: ["held"]));

        // Act
        var result = await _loader.LoadAsync(SeasonId, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Options.Select(o => o.Id).ShouldBe(["early", "late"]);
        result.Value.Options[0].TeamSize.ShouldBe(3);
        result.Value.HeldElsewhere.ShouldBe(1);
    }

    [Fact(DisplayName = "LoadAsync should keep the tournaments of the Series being edited")]
    public async Task LoadAsync_ShouldKeepEditedSeriesTournaments()
    {
        // Arrange
        SetupTournaments(Tournament("mine", new DateOnly(2025, 1, 1)));
        SetupSeriesList(SeriesSummaryResponseFactory.Create(id: "editing", seasonId: SeasonId, stageTournamentIds: ["mine"]));

        // Act
        var result = await _loader.LoadAsync(SeasonId, "editing", TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Options.Select(o => o.Id).ShouldBe(["mine"]);
        result.Value.HeldElsewhere.ShouldBe(0);
    }

    [Fact(DisplayName = "LoadAsync should ignore Series from other seasons")]
    public async Task LoadAsync_ShouldIgnoreOtherSeasons()
    {
        // Arrange
        SetupTournaments(Tournament("t1", new DateOnly(2025, 1, 1)));
        SetupSeriesList(SeriesSummaryResponseFactory.Create(id: "elsewhere", seasonId: "season-2", stageTournamentIds: ["t1"]));

        // Act
        var result = await _loader.LoadAsync(SeasonId, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Options.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "LoadAsync should return the error when the tournaments call fails")]
    public async Task LoadAsync_ShouldReturnError_WhenTournamentsFail()
    {
        // Arrange
        var response = Own(new StubApiResponse<CollectionResponse<SeasonTournamentResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.InternalServerError,
        });
        _seasonsApi
            .Setup(x => x.ListTournamentsInSeasonAsync(SeasonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _loader.LoadAsync(SeasonId, null, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }
}