using FastEndpoints;

using Neba.Api.HallOfFame.ListHallOfFameInductions;
using Neba.Application.HallOfFame.ListHallOfFameInductions;
using Neba.Application.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.HallOfFame;

namespace Neba.Api.Tests.HallOfFame.ListHallOfFameInductions;

[UnitTest]
[Component("HallOfFame")]
public sealed class ListHallOfFameInductionsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped inductions when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedInductions_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = HallOfFameInductionDtoFactory.Bogus(3, 11);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHallOfFameInductionsQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListHallOfFameInductionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(dtos.Count);
        
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no inductions exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoInductionsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHallOfFameInductionsQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListHallOfFameInductionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}
