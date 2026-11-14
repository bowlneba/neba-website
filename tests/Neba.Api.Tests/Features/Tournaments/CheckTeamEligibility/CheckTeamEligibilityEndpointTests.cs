using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.CheckTeamEligibility;

[UnitTest]
[Component("Tournaments")]
public sealed class CheckTeamEligibilityEndpointTests
{
    private static Mock<IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>>> HandlerReturning(
        ErrorOr<TeamEligibilityDto> result, CancellationToken ct, Action<CheckTeamEligibilityQuery>? capture = null)
    {
        var mock = new Mock<IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>>>(MockBehavior.Strict);
        mock.Setup(handler => handler.HandleAsync(It.IsAny<CheckTeamEligibilityQuery>(), ct))
            .Callback<CheckTeamEligibilityQuery, CancellationToken>((query, _) => capture?.Invoke(query))
            .ReturnsAsync(result);
        return mock;
    }

    [Fact(DisplayName = "HandleAsync should pass the route and query values to the handler")]
    public async Task HandleAsync_ShouldBuildQueryFromRequest()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournamentId = TournamentId.New();
        var bowlerId = BowlerId.New();
        CheckTeamEligibilityQuery? captured = null;
        var handler = HandlerReturning(
            new TeamEligibilityDto(TeamEligibilityOutcome.Eligible, [], []), ct, query => captured = query);
        var endpoint = Factory.Create<CheckTeamEligibilityEndpoint>(handler.Object);

        // Act
        await endpoint.HandleAsync(
            new CheckTeamEligibilityRequest
            {
                Id = tournamentId.Value.ToString(),
                BowlerIds = [bowlerId.Value.ToString()],
                UnknownBowlerCount = 1
            },
            ct);

        // Assert
        captured.ShouldNotBeNull();
        captured.TournamentId.ShouldBe(tournamentId);
        captured.BowlerIds.ShouldBe([bowlerId]);
        captured.UnknownBowlerCount.ShouldBe(1);
    }

    [Fact(DisplayName = "HandleAsync should map an Ineligible result with its reasons")]
    public async Task HandleAsync_ShouldMapIneligibleResult()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var dto = new TeamEligibilityDto(
            TeamEligibilityOutcome.Ineligible,
            [TeamEligibilityReasons.TooManyChampions(1)],
            []);
        var endpoint = Factory.Create<CheckTeamEligibilityEndpoint>(HandlerReturning(dto, ct).Object);

        // Act
        await endpoint.HandleAsync(new CheckTeamEligibilityRequest { Id = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.Response.Outcome.ShouldBe("Ineligible");
        endpoint.Response.Reasons.ShouldHaveSingleItem().Code.ShouldBe("Champions.TooMany");
        endpoint.Response.DateChecks.ShouldBeEmpty();
        endpoint.Response.DefendingSource.ShouldBeNull();
        endpoint.Response.ProfessionalTitlesVerified.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should map the per-date answers of a DependsOnDate result")]
    public async Task HandleAsync_ShouldMapDateChecks()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var start = new DateOnly(2026, 3, 7);
        var end = new DateOnly(2026, 3, 8);
        var dto = new TeamEligibilityDto(
            TeamEligibilityOutcome.DependsOnDate,
            [],
            [
                new TeamEligibilityCheck(start, []),
                new TeamEligibilityCheck(end, [TeamEligibilityReasons.SameAgeGroup])
            ]);
        var endpoint = Factory.Create<CheckTeamEligibilityEndpoint>(HandlerReturning(dto, ct).Object);

        // Act
        await endpoint.HandleAsync(new CheckTeamEligibilityRequest { Id = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.Response.Outcome.ShouldBe("DependsOnDate");
        endpoint.Response.DateChecks.Count.ShouldBe(2);
        var second = endpoint.Response.DateChecks.Last();
        second.AsOf.ShouldBe(end);
        second.Eligible.ShouldBeFalse();
        second.Reasons.ShouldHaveSingleItem().Code.ShouldBe("Age.SameAgeGroup");
    }

    [Fact(DisplayName = "HandleAsync should map the source tournament of a DefendingChampions result")]
    public async Task HandleAsync_ShouldMapDefendingSource()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var team = new DefendingTeam(
            TournamentId.New(), "Joe's Doubles", "2025 Season", SourceTruncated: true, Undefended: false, Champions: []);
        var dto = new TeamEligibilityDto(TeamEligibilityOutcome.DefendingChampions, [], [], team);
        var endpoint = Factory.Create<CheckTeamEligibilityEndpoint>(HandlerReturning(dto, ct).Object);

        // Act
        await endpoint.HandleAsync(new CheckTeamEligibilityRequest { Id = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.Response.Outcome.ShouldBe("DefendingChampions");
        var source = endpoint.Response.DefendingSource.ShouldNotBeNull();
        source.TournamentName.ShouldBe("Joe's Doubles");
        source.SeasonDescription.ShouldBe("2025 Season");
        source.SourceTruncated.ShouldBeTrue();
        source.Undefended.ShouldBeFalse();
    }

    [Theory(DisplayName = "HandleAsync should map each error type to its status code")]
    [InlineData(ErrorType.NotFound, 404, TestDisplayName = "NotFound")]
    [InlineData(ErrorType.Validation, 422, TestDisplayName = "Validation")]
    [InlineData(ErrorType.Failure, 500, TestDisplayName = "Failure")]
    public async Task HandleAsync_ShouldMapErrorToStatusCode(ErrorType type, int expectedStatus)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        Error error = type switch
        {
            ErrorType.NotFound => Error.NotFound("Some.NotFound", "Not found."),
            ErrorType.Validation => Error.Validation("Some.Invalid", "Invalid."),
            _ => Error.Failure("Some.Failure", "Failed.")
        };
        var endpoint = Factory.Create<CheckTeamEligibilityEndpoint>(HandlerReturning(error, ct).Object);

        // Act
        await endpoint.HandleAsync(new CheckTeamEligibilityRequest { Id = TournamentId.New().Value.ToString() }, ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(expectedStatus);
    }

    [Fact(DisplayName = "Configure should register an anonymous GET route under /tournaments")]
    public void Configure_ShouldRegisterAnonymousGetRoute()
    {
        // Arrange
        var handler = new Mock<IQueryHandler<CheckTeamEligibilityQuery, ErrorOr<TeamEligibilityDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<CheckTeamEligibilityEndpoint>(handler.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(route => route.Contains("tournaments") && route.Contains("team-eligibility"));
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
