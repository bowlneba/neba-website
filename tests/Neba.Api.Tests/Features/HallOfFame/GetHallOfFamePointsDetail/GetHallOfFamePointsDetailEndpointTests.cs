using ErrorOr;

using FastEndpoints;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.GetHallOfFamePointsDetail;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

namespace Neba.Api.Tests.Features.HallOfFame.GetHallOfFamePointsDetail;

[UnitTest]
[Component("HallOfFame")]
public sealed class GetHallOfFamePointsDetailEndpointTests
{
    private const string BowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";

    private static Mock<IQueryHandler<GetHallOfFamePointsDetailQuery, ErrorOr<HallOfFamePointsDetailDto>>> StrictHandler()
        => new(MockBehavior.Strict);

    [Fact(DisplayName = "HandleAsync should return OK with the mapped detail when the query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedDetail_WhenQuerySucceeds()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var dto = HallOfFamePointsDetailDtoFactory.Create(
            bowlerId: BowlerId,
            bowlerName: "Ray Dunleavy",
            inductedCategories: ["Meritorious Service"],
            membershipHistoryComplete: false);

        var handlerMock = StrictHandler();
        handlerMock
            .Setup(handler => handler.HandleAsync(
                It.Is<GetHallOfFamePointsDetailQuery>(query => query.BowlerId == new BowlerId(BowlerId)),
                cancellationToken))
            .ReturnsAsync(dto);

        var endpoint = Factory.Create<GetHallOfFamePointsDetailEndpoint>(handlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetHallOfFamePointsDetailRequest { BowlerId = BowlerId }, cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return 404 when the bowler is not found")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenBowlerNotFound()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var handlerMock = StrictHandler();
        handlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<GetHallOfFamePointsDetailQuery>(), cancellationToken))
            .ReturnsAsync(BowlerErrors.NotFound(new BowlerId(BowlerId)));

        var endpoint = Factory.Create<GetHallOfFamePointsDetailEndpoint>(handlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetHallOfFamePointsDetailRequest { BowlerId = BowlerId }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }

    [Fact(DisplayName = "HandleAsync should return 500 with the error when the query fails unexpectedly")]
    public async Task HandleAsync_ShouldReturnServerError_WhenQueryFailsUnexpectedly()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var handlerMock = StrictHandler();
        handlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<GetHallOfFamePointsDetailQuery>(), cancellationToken))
            .ReturnsAsync(Error.Failure("HallOfFame.Failure", "Something broke."));

        var endpoint = Factory.Create<GetHallOfFamePointsDetailEndpoint>(handlerMock.Object);

        // Act
        await endpoint.HandleAsync(new GetHallOfFamePointsDetailRequest { BowlerId = BowlerId }, cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /hall-of-fame/points/{bowlerId}")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var endpoint = Factory.Create<GetHallOfFamePointsDetailEndpoint>(StrictHandler().Object);

        // Act & Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/hall-of-fame/points/{bowlerId}");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}