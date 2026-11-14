using FastEndpoints;

using Neba.Api.Features.Seasons.Domain;

using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentsInSeason;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentsInSeasonEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped tournaments when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedTournaments_WhenQuerySucceeds()
    {
        // Arrange
        var seasonId = new SeasonId("01000000000000000000000001");
        var dtos = SeasonTournamentDtoFactory.Bogus(3, 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.Is<ListTournamentsInSeasonQuery>(q => q.SeasonId == seasonId), cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListTournamentsInSeasonEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListTournamentsInSeasonRequest { SeasonId = seasonId.Value.ToString() }, cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should map team size and defending champions")]
    public async Task HandleAsync_ShouldMapTeamSizeAndDefendingChampions()
    {
        // Arrange
        var seasonId = new SeasonId("01000000000000000000000001");
        var cancellationToken = TestContext.Current.CancellationToken;
        var dto = SeasonTournamentDtoFactory.Create(
            teamSize: 2,
            defendingChampions:
            [
                DefendingTeamDtoFactory.Create(sourceTruncated: true),
                DefendingTeamDtoFactory.Create(tournamentName: "Older Open", undefended: true)
            ]);

        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListTournamentsInSeasonQuery>(), cancellationToken))
            .ReturnsAsync([dto]);
        var endpoint = Factory.Create<ListTournamentsInSeasonEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListTournamentsInSeasonRequest { SeasonId = seasonId.Value.ToString() }, cancellationToken);

        // Assert
        var response = endpoint.Response.Items.ShouldHaveSingleItem();
        response.TeamSize.ShouldBe(2);
        response.DefendingChampions.Count.ShouldBe(2);
        var first = response.DefendingChampions.First();
        first.TournamentName.ShouldBe("Joe's Pizza Open");
        first.SeasonDescription.ShouldBe("2025 Season");
        first.SourceTruncated.ShouldBeTrue();
        first.Champions.ShouldBe(["Dave Smith", "Dana Jones"]);
        response.DefendingChampions.Last().Undefended.ShouldBeTrue();
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /seasons/{seasonId}/tournaments")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListTournamentsInSeasonEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/seasons/{seasonId}/tournaments");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no tournaments exist for season")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoTournamentsExist()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListTournamentsInSeasonQuery>(), cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListTournamentsInSeasonEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListTournamentsInSeasonRequest { SeasonId = seasonId.Value.ToString() }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should pass the SeasonId from the request to the query")]
    public async Task HandleAsync_ShouldPassSeasonIdToQuery()
    {
        // Arrange
        var seasonId = new SeasonId("01000000000000000000000002");
        var cancellationToken = TestContext.Current.CancellationToken;

        ListTournamentsInSeasonQuery? capturedQuery = null;

        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListTournamentsInSeasonQuery>(), cancellationToken))
            .Callback<ListTournamentsInSeasonQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListTournamentsInSeasonEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(new ListTournamentsInSeasonRequest { SeasonId = seasonId.Value.ToString() }, cancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.SeasonId.ShouldBe(seasonId);
    }
}