using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Seasons.ListSeasons;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.ListChampions;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments;
using Neba.Website.Server.Tournaments.Schedule;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.TournamentApiService")]
public sealed class TournamentApiServiceTests
{
    private readonly Mock<ISeasonsApi> _mockSeasonsApi;
    private readonly Mock<ITournamentsApi> _mockTournamentsApi;
    private readonly TournamentApiService _service;

    public TournamentApiServiceTests()
    {
        _mockSeasonsApi = new Mock<ISeasonsApi>(MockBehavior.Strict);
        _mockTournamentsApi = new Mock<ITournamentsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var executor = new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance);

        _service = new TournamentApiService(executor, _mockSeasonsApi.Object, _mockTournamentsApi.Object);
    }

    // ── GetChampionsDataAsync ─────────────────────────────────────────────

    [Fact(DisplayName = "GetChampionsDataAsync should return summaries and years when API succeeds")]
    public async Task GetChampionsDataAsync_ShouldReturnSummariesAndYears_WhenApiSucceeds()
    {
        // Arrange
        var champion = TournamentChampionResponseFactory.Create();
        SetupChampionsSuccess([champion]);

        // Act
        var result = await _service.GetChampionsDataAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Summaries.ShouldNotBeNull();
        result.Value.Years.ShouldNotBeNull();
    }

    [Fact(DisplayName = "GetChampionsDataAsync should return empty collections when API returns no items")]
    public async Task GetChampionsDataAsync_ShouldReturnEmptyCollections_WhenApiReturnsNoItems()
    {
        // Arrange
        SetupChampionsSuccess([]);

        // Act
        var result = await _service.GetChampionsDataAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Summaries.ShouldBeEmpty();
        result.Value.Years.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetChampionsDataAsync should return error when API fails")]
    public async Task GetChampionsDataAsync_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        SetupChampionsFailure(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await _service.GetChampionsDataAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    // ── GetSeasonsAsync ───────────────────────────────────────────────────

    [Fact(DisplayName = "GetSeasonsAsync should return mapped season view models when API succeeds")]
    public async Task GetSeasonsAsync_ShouldReturnMappedSeasonViewModels_WhenApiSucceeds()
    {
        // Arrange
        var season = new SeasonResponse
        {
            Id = "01000000000000000000000001",
            Description = "2024-2025 Season",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 6, 30),
        };
        SetupSeasonsSuccess([season]);

        // Act
        var result = await _service.GetSeasonsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldHaveSingleItem();
        var vm = result.Value.Single();
        vm.Id.ShouldBe(season.Id);
        vm.Description.ShouldBe(season.Description);
        vm.StartDate.ShouldBe(season.StartDate);
        vm.EndDate.ShouldBe(season.EndDate);
    }

    [Fact(DisplayName = "GetSeasonsAsync should return empty list when API returns no seasons")]
    public async Task GetSeasonsAsync_ShouldReturnEmptyList_WhenApiReturnsNoSeasons()
    {
        // Arrange
        SetupSeasonsSuccess([]);

        // Act
        var result = await _service.GetSeasonsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetSeasonsAsync should return error when API fails")]
    public async Task GetSeasonsAsync_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        SetupSeasonsFailure(System.Net.HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await _service.GetSeasonsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    // ── GetTournamentsForSeasonAsync ──────────────────────────────────────

    [Fact(DisplayName = "GetTournamentsForSeasonAsync should return mapped tournament view models when API succeeds")]
    public async Task GetTournamentsForSeasonAsync_ShouldReturnMappedViewModels_WhenApiSucceeds()
    {
        // Arrange
        var season = new SeasonViewModel
        {
            Id = "01000000000000000000000001",
            Description = "2024-2025 Season",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 6, 30),
        };

        var tournament = new SeasonTournamentResponse
        {
            Id = "01000000000000000000000002",
            Name = "NEBA Singles",
            StartDate = new DateOnly(2024, 11, 1),
            EndDate = new DateOnly(2024, 11, 1),
            TournamentType = "Singles",
        };
        SetupTournamentsInSeasonSuccess(season.Id, [tournament]);

        // Act
        var result = await _service.GetTournamentsForSeasonAsync(season, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldHaveSingleItem();
        var vm = result.Value.Single();
        vm.Id.ShouldBe(tournament.Id);
        vm.Name.ShouldBe(tournament.Name);
        vm.TournamentType.ShouldBe(tournament.TournamentType);
    }

    [Fact(DisplayName = "GetTournamentsForSeasonAsync should return empty list when season has no tournaments")]
    public async Task GetTournamentsForSeasonAsync_ShouldReturnEmptyList_WhenSeasonHasNoTournaments()
    {
        // Arrange
        var season = new SeasonViewModel
        {
            Id = "01000000000000000000000001",
            Description = "2024-2025 Season",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 6, 30),
        };
        SetupTournamentsInSeasonSuccess(season.Id, []);

        // Act
        var result = await _service.GetTournamentsForSeasonAsync(season, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetTournamentsForSeasonAsync should return error when API fails")]
    public async Task GetTournamentsForSeasonAsync_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        var season = new SeasonViewModel
        {
            Id = "01000000000000000000000001",
            Description = "2024-2025 Season",
            StartDate = new DateOnly(2024, 9, 1),
            EndDate = new DateOnly(2025, 6, 30),
        };
        SetupTournamentsInSeasonFailure(season.Id, System.Net.HttpStatusCode.NotFound);

        // Act
        var result = await _service.GetTournamentsForSeasonAsync(season, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    [Fact(DisplayName = "GetTournamentsForSeasonAsync should pass season label to mapped view model")]
    public async Task GetTournamentsForSeasonAsync_ShouldPassSeasonLabelToViewModel()
    {
        // Arrange — single-year season so label is just the year
        var season = new SeasonViewModel
        {
            Id = "01000000000000000000000001",
            Description = "2025 Season",
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 12, 31),
        };

        var tournament = new SeasonTournamentResponse
        {
            Id = "01000000000000000000000002",
            Name = "Singles Classic",
            StartDate = new DateOnly(2025, 3, 1),
            EndDate = new DateOnly(2025, 3, 1),
            TournamentType = "Singles",
        };
        SetupTournamentsInSeasonSuccess(season.Id, [tournament]);

        // Act
        var result = await _service.GetTournamentsForSeasonAsync(season, TestContext.Current.CancellationToken);

        // Assert
        result.Value.Single().Season.ShouldBe("2025");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetupChampionsSuccess(IReadOnlyCollection<TournamentChampionResponse> items)
    {
        using var response = new StubApiResponse<CollectionResponse<TournamentChampionResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<TournamentChampionResponse> { Items = items }
        };

        _mockTournamentsApi
            .Setup(x => x.ListTournamentChampionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupChampionsFailure(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<CollectionResponse<TournamentChampionResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (CollectionResponse<TournamentChampionResponse>?)null
        };

        _mockTournamentsApi
            .Setup(x => x.ListTournamentChampionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupSeasonsSuccess(IReadOnlyCollection<SeasonResponse> items)
    {
        using var response = new StubApiResponse<CollectionResponse<SeasonResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<SeasonResponse> { Items = items }
        };

        _mockSeasonsApi
            .Setup(x => x.ListSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupSeasonsFailure(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<CollectionResponse<SeasonResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (CollectionResponse<SeasonResponse>?)null
        };

        _mockSeasonsApi
            .Setup(x => x.ListSeasonsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupTournamentsInSeasonSuccess(string seasonId, IReadOnlyCollection<SeasonTournamentResponse> items)
    {
        using var response = new StubApiResponse<CollectionResponse<SeasonTournamentResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<SeasonTournamentResponse> { Items = items }
        };

        _mockSeasonsApi
            .Setup(x => x.ListTournamentsInSeasonAsync(seasonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupTournamentsInSeasonFailure(string seasonId, System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<CollectionResponse<SeasonTournamentResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (CollectionResponse<SeasonTournamentResponse>?)null
        };

        _mockSeasonsApi
            .Setup(x => x.ListTournamentsInSeasonAsync(seasonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}