using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Tournaments.Domain;

using Neba.Api.Features.Tournaments.GetTournament;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.GetTournament;

[UnitTest]
[Component("Tournaments")]
public sealed class GetTournamentEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped tournament detail when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedTournamentDetail_WhenQuerySucceeds()
    {
        // Arrange
        var dto = TournamentDetailDtoFactory.Bogus(count: 1, seed: 42).Single();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(
            new GetTournamentRequest { TournamentId = dto.Id.Value.ToString() },
            cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should map the Series a tournament is a Stage of, and leave it null otherwise")]
    public async Task HandleAsync_ShouldMapSeries()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var series = TournamentSeriesDtoFactory.Create(name: "VH", kind: "Survivor", advancingCount: 20);
        var withSeries = TournamentDetailDtoFactory.Create(series: series);
        var without = TournamentDetailDtoFactory.Create();

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .SetupSequence(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(withSeries)
            .ReturnsAsync(without);
        var first = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);
        var second = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await first.HandleAsync(new GetTournamentRequest { TournamentId = withSeries.Id.Value.ToString() }, cancellationToken);
        await second.HandleAsync(new GetTournamentRequest { TournamentId = without.Id.Value.ToString() }, cancellationToken);

        // Assert
        first.Response.Series.ShouldNotBeNull();
        first.Response.Series.SeriesId.ShouldBe(series.SeriesId.Value.ToString());
        first.Response.Series.Name.ShouldBe("VH");
        first.Response.Series.Kind.ShouldBe("Survivor");
        first.Response.Series.AdvancingCount.ShouldBe(20);
        second.Response.Series.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync should map team size, previous tournament and defending champions")]
    public async Task HandleAsync_ShouldMapTeamSizePreviousTournamentAndDefendingChampions()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var previousId = TournamentId.New();
        var dto = TournamentDetailDtoFactory.Create(
            teamSize: 3,
            defendingChampions: [DefendingTeamDtoFactory.Create(sourceTruncated: true)],
            previousTournamentId: previousId);

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(dto);
        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetTournamentRequest { TournamentId = dto.Id.Value.ToString() }, cancellationToken);

        // Assert
        endpoint.Response.TeamSize.ShouldBe(3);
        endpoint.Response.PreviousTournamentId.ShouldBe(previousId.Value.ToString());
        var team = endpoint.Response.DefendingChampions.ShouldHaveSingleItem();
        team.TournamentName.ShouldBe("Joe's Pizza Open");
        team.SourceTruncated.ShouldBeTrue();
        team.Champions.ShouldBe(["Dave Smith", "Dana Jones"]);
    }

    [Fact(DisplayName = "HandleAsync should return 404 Not Found when tournament does not exist")]
    public async Task HandleAsync_ShouldReturn404_WhenTournamentDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(Error.NotFound());

        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(
            new GetTournamentRequest { TournamentId = TournamentId.New().Value.ToString() },
            cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 and add all error messages to validation failures when query returns non-NotFound errors")]
    public async Task HandleAsync_ShouldReturn500WithAllErrors_WhenQueryReturnsNonNotFoundErrors()
    {
        // Arrange — two errors to exercise the foreach loop body
        const string firstErrorMessage = "An unexpected error occurred.";
        const string secondErrorMessage = "A secondary failure occurred.";
        var cancellationToken = TestContext.Current.CancellationToken;

        List<Error> errors =
        [
            Error.Unexpected(description: firstErrorMessage),
            Error.Failure(description: secondErrorMessage),
        ];

        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<GetTournamentQuery>(), cancellationToken))
            .ReturnsAsync(errors);

        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(
            new GetTournamentRequest { TournamentId = TournamentId.New().Value.ToString() },
            cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route under /tournaments")]
    public void Configure_ShouldRegisterAnonymousGetRoute_UnderTournamentsPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<GetTournamentEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments"), "should be under the /tournaments path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}