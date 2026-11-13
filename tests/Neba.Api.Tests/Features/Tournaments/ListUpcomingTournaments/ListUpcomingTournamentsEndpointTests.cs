using FastEndpoints;

using Neba.Api.Features.Tournaments.ListTournamentsInSeason;
using Neba.Api.Features.Tournaments.ListUpcomingTournaments;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListUpcomingTournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class ListUpcomingTournamentsEndpointTests
{
    [Fact(DisplayName = "Configure should register anonymous GET route at /tournaments/upcoming")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListUpcomingTournamentsQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListUpcomingTournamentsEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/tournaments/upcoming");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with the mapped tournaments in handler order")]
    public async Task HandleAsync_ShouldReturnOkWithMappedTournaments_InHandlerOrder()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = SeasonTournamentDtoFactory.Create(name: "First", entryFee: 125m);
        var second = SeasonTournamentDtoFactory.Create(name: "Second");

        var queryHandlerMock = new Mock<IQueryHandler<ListUpcomingTournamentsQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListUpcomingTournamentsQuery>(), ct))
            .ReturnsAsync([first, second]);
        var endpoint = Factory.Create<ListUpcomingTournamentsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.TotalItems.ShouldBe(2);
        endpoint.Response.Items.Select(i => i.Name).ShouldBe(["First", "Second"]);
        endpoint.Response.Items.First().Id.ShouldBe(first.Id.Value.ToString());
        endpoint.Response.Items.First().EntryFee.ShouldBe(125m);
    }

    [Fact(DisplayName = "HandleAsync should return OK with an empty collection when nothing is upcoming")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNothingIsUpcoming()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var queryHandlerMock = new Mock<IQueryHandler<ListUpcomingTournamentsQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListUpcomingTournamentsQuery>(), ct))
            .ReturnsAsync([]);
        var endpoint = Factory.Create<ListUpcomingTournamentsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Items.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should query as an anonymous caller when the request has no user")]
    public async Task HandleAsync_ShouldQueryAsAnonymousCaller_WhenRequestHasNoUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        ListUpcomingTournamentsQuery? captured = null;

        var queryHandlerMock = new Mock<IQueryHandler<ListUpcomingTournamentsQuery, IReadOnlyCollection<SeasonTournamentDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListUpcomingTournamentsQuery>(), ct))
            .Callback<ListUpcomingTournamentsQuery, CancellationToken>((q, _) => captured = q)
            .ReturnsAsync([]);
        var endpoint = Factory.Create<ListUpcomingTournamentsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        captured.ShouldNotBeNull();
        captured.CallerIsAuthenticated.ShouldBeFalse();
        captured.CallerHasTournamentManagementPermission.ShouldBeFalse();
    }
}
