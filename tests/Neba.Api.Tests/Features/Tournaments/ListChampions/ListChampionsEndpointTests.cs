using FastEndpoints;

using Neba.Api.Features.Tournaments.ListChampions;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListChampions;

[UnitTest]
[Component("Tournaments")]
public sealed class ListChampionsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped champions when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedChampions_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = TournamentChampionDtoFactory.Bogus(3, 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListChampionsQuery>(), cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListChampionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /tournaments/champions")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListChampionsEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/tournaments/champions");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no champions exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoChampionsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListChampionsQuery>(), cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListChampionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should map TournamentId, BowlerId, and HallOfFame fields correctly")]
    public async Task HandleAsync_ShouldMapFields_Correctly()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var champion = ChampionDtoFactory.Create(hallOfFame: true);
        var dto = TournamentChampionDtoFactory.Create(champions: [champion]);

        var queryHandlerMock = new Mock<IQueryHandler<ListChampionsQuery, IReadOnlyCollection<TournamentChampionsDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListChampionsQuery>(), cancellationToken))
            .ReturnsAsync([dto]);

        var endpoint = Factory.Create<ListChampionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        var item = endpoint.Response.Items.ShouldHaveSingleItem();
        item.TournamentId.ShouldBe(dto.TournamentId.Value.ToString());
        item.TournamentName.ShouldBe(dto.TournamentName);
        item.TournamentDate.ShouldBe(dto.TournamentDate);
        item.TournamentType.ShouldBe(dto.TournamentType);
        var responseChampion = item.Champions.ShouldHaveSingleItem();
        responseChampion.BowlerId.ShouldBe(champion.BowlerId.Value.ToString());
        responseChampion.BowlerName.ShouldBe(champion.BowlerName.ToDisplayName());
        responseChampion.HallOfFame.ShouldBeTrue();
    }
}