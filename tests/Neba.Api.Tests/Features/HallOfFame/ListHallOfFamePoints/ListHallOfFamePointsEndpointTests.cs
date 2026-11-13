using FastEndpoints;

using Neba.Api.Features.HallOfFame.ListHallOfFamePoints;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

namespace Neba.Api.Tests.Features.HallOfFame.ListHallOfFamePoints;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFamePointsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with the mapped rules, thresholds and standings")]
    public async Task HandleAsync_ShouldReturnOkWithMappedPoints_WhenQuerySucceeds()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var dto = HallOfFamePointsDtoFactory.Create(
            standings:
            [
                HallOfFamePointsStandingDtoFactory.Create(
                    bowlerId: "01JZ8K3M4N5P6Q7R8S9T0V1W3Z",
                    bowlerName: "Ray Dunleavy",
                    totalPoints: 42,
                    seasonsWithPoints: 5,
                    membershipYears: 28,
                    membershipHistoryComplete: false,
                    eligible: true,
                    meetsPoints: true,
                    meetsSeasons: true,
                    inductedCategories: ["Meritorious Service"])
            ]);

        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListHallOfFamePointsQuery>(), cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<ListHallOfFamePointsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return OK with an empty standings list when nobody has points")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyStandings_WhenNoBowlerHasPoints()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListHallOfFamePointsQuery>(), cancellationToken))
            .ReturnsAsync(HallOfFamePointsDtoFactory.Create(standings: []));

        var endpoint = Factory.Create<ListHallOfFamePointsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.Standings.ShouldBeEmpty();
        endpoint.Response.Rules.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /hall-of-fame/points")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFamePointsQuery, HallOfFamePointsDto>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListHallOfFamePointsEndpoint>(queryHandlerMock.Object);

        // Act & Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/hall-of-fame/points");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
