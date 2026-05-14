using FastEndpoints;

using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Messaging;
using Neba.Domain.Seasons;
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
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.HttpContext.Response.ContentType.ShouldNotBeNull();
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(dtos.Count);

        await Verify(endpoint.Response);
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