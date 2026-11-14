using ErrorOr;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Bowlers;
using Neba.Api.Contracts.Bowlers.SearchBowlers;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;
using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Tournaments.TeamEligibility;
using Neba.Website.Tests.TestSupport;

using Refit.Testing;

namespace Neba.Website.Tests.Tournaments.TeamEligibility;

[UnitTest]
[Component("Website.Tournaments.TeamEligibilityService")]
public sealed class TeamEligibilityServiceTests
{
    private const string TournamentId = "01J7ZK8X6ZQJ8V3F8N9T9C9R2E";

    private readonly Mock<ITournamentsApi> _mockTournamentsApi = new(MockBehavior.Strict);
    private readonly Mock<IBowlersApi> _mockBowlersApi = new(MockBehavior.Strict);
    private readonly TeamEligibilityService _service;

    public TeamEligibilityServiceTests()
    {
        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        var executor = new ApiExecutor(mockStopwatch.Object, new StubNavigationManager(), NullLogger<ApiExecutor>.Instance);
        _service = new TeamEligibilityService(executor, _mockTournamentsApi.Object, _mockBowlersApi.Object);
    }

    private void SetupCheck(TeamEligibilityResponse? content, bool success = true, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK)
    {
        using var response = new StubApiResponse<TeamEligibilityResponse>
        {
            IsSuccessStatusCode = success,
            StatusCode = status,
            Content = content
        };

        _mockTournamentsApi
            .Setup(x => x.CheckTeamEligibilityAsync(TournamentId, It.IsAny<IEnumerable<string>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupSearch(IReadOnlyCollection<BowlerSearchResponse>? items, bool success = true)
    {
        using var response = new StubApiResponse<CollectionResponse<BowlerSearchResponse>>
        {
            IsSuccessStatusCode = success,
            StatusCode = success ? System.Net.HttpStatusCode.OK : System.Net.HttpStatusCode.InternalServerError,
            Content = items is null ? null : new CollectionResponse<BowlerSearchResponse> { Items = items }
        };

        _mockBowlersApi
            .Setup(x => x.SearchBowlersAsync("smith", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    [Fact(DisplayName = "CheckAsync should map an ineligible answer with its reason messages")]
    public async Task CheckAsync_ShouldMapIneligibleAnswer_WhenApiSucceeds()
    {
        // Arrange
        SetupCheck(new TeamEligibilityResponse
        {
            Outcome = "Ineligible",
            Reasons = [new TeamEligibilityReasonResponse { Code = "Champions.TooMany", Message = "Too many champions." }]
        });

        // Act
        var result = await _service.CheckAsync(TournamentId, ["a", "b"], 0, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcomeKind.Ineligible);
        result.Value.Reasons.ShouldBe(["Too many champions."]);
        result.Value.ShowProfessionalTitlesNote.ShouldBeFalse();
    }

    [Fact(DisplayName = "CheckAsync should map the date checks of a DependsOnDate answer")]
    public async Task CheckAsync_ShouldMapDateChecks_WhenOutcomeDependsOnDate()
    {
        // Arrange
        SetupCheck(new TeamEligibilityResponse
        {
            Outcome = "DependsOnDate",
            DateChecks =
            [
                new TeamEligibilityDateCheckResponse { AsOf = new DateOnly(2026, 3, 7), Eligible = true },
                new TeamEligibilityDateCheckResponse
                {
                    AsOf = new DateOnly(2026, 3, 8),
                    Eligible = false,
                    Reasons = [new TeamEligibilityReasonResponse { Code = "Age.SameAgeGroup", Message = "Same group." }]
                }
            ]
        });

        // Act
        var result = await _service.CheckAsync(TournamentId, ["a", "b"], 0, TestContext.Current.CancellationToken);

        // Assert
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcomeKind.DependsOnDate);
        result.Value.DateChecks.Count.ShouldBe(2);
        result.Value.DateChecks[1].Reasons.ShouldBe(["Same group."]);
        result.Value.ShowProfessionalTitlesNote.ShouldBeTrue();
    }

    [Fact(DisplayName = "CheckAsync should map the source of a defending team")]
    public async Task CheckAsync_ShouldMapDefendingSource_WhenOutcomeIsDefendingChampions()
    {
        // Arrange
        SetupCheck(new TeamEligibilityResponse
        {
            Outcome = "DefendingChampions",
            DefendingSource = new DefendingSourceResponse
            {
                TournamentName = "Joe's Doubles",
                SeasonDescription = "2025 Season",
                SourceTruncated = true,
                Undefended = false
            }
        });

        // Act
        var result = await _service.CheckAsync(TournamentId, ["a", "b"], 0, TestContext.Current.CancellationToken);

        // Assert
        result.Value.Outcome.ShouldBe(TeamEligibilityOutcomeKind.DefendingChampions);
        var source = result.Value.DefendingSource.ShouldNotBeNull();
        source.TournamentName.ShouldBe("Joe's Doubles");
        source.SourceTruncated.ShouldBeTrue();
    }

    [Theory(DisplayName = "CheckAsync should return an error for an outcome the site does not know")]
    [InlineData("Maybe", TestDisplayName = "Unknown name")]
    [InlineData("7", TestDisplayName = "Number outside the enum")]
    [InlineData("", TestDisplayName = "Empty")]
    public async Task CheckAsync_ShouldReturnError_WhenOutcomeIsUnknown(string outcome)
    {
        // Arrange
        SetupCheck(new TeamEligibilityResponse { Outcome = outcome });

        // Act
        var result = await _service.CheckAsync(TournamentId, ["a", "b"], 0, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("TeamEligibility.UnknownOutcome");
    }

    [Fact(DisplayName = "CheckAsync should return the API error when the request fails")]
    public async Task CheckAsync_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        SetupCheck(null, success: false, status: System.Net.HttpStatusCode.InternalServerError);

        // Act
        var result = await _service.CheckAsync(TournamentId, ["a", "b"], 0, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(ErrorType.Failure);
    }

    [Fact(DisplayName = "SearchBowlersAsync should map each bowler to a view model with a display text")]
    public async Task SearchBowlersAsync_ShouldMapBowlers_WhenApiSucceeds()
    {
        // Arrange
        SetupSearch(
        [
            new BowlerSearchResponse { BowlerId = "id1", Name = "Dave Smith", UsbcId = "4821-09317" },
            new BowlerSearchResponse { BowlerId = "id2", Name = "Dana Smith" }
        ]);

        // Act
        var result = await _service.SearchBowlersAsync("smith", TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Select(bowler => bowler.DisplayText).ShouldBe(["Dave Smith (4821-09317)", "Dana Smith"]);
        result.Value[0].BowlerId.ShouldBe("id1");
    }

    [Fact(DisplayName = "SearchBowlersAsync should return an empty list when nobody matches")]
    public async Task SearchBowlersAsync_ShouldReturnEmpty_WhenNobodyMatches()
    {
        // Arrange
        SetupSearch([]);

        // Act
        var result = await _service.SearchBowlersAsync("smith", TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "SearchBowlersAsync should return the API error when the request fails")]
    public async Task SearchBowlersAsync_ShouldReturnError_WhenApiFails()
    {
        // Arrange
        SetupSearch(null, success: false);

        // Act
        var result = await _service.SearchBowlersAsync("smith", TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
    }
}
