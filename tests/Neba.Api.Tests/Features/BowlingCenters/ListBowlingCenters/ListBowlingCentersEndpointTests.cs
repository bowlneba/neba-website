using FastEndpoints;

using Neba.Api.Features.BowlingCenters.ListBowlingCenters;
using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;

namespace Neba.Api.Tests.Features.BowlingCenters.ListBowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class ListBowlingCentersEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped bowling centers when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedBowlingCenters_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = BowlingCenterSummaryDtoFactory.Bogus(3, 28);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListBowlingCentersQuery, IReadOnlyCollection<BowlingCenterSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListBowlingCentersQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListBowlingCentersEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.HttpContext.Response.ContentType.ShouldNotBeNull();
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(dtos.Count);
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /bowling-centers")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListBowlingCentersQuery, IReadOnlyCollection<BowlingCenterSummaryDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListBowlingCentersEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("bowling-centers"), "should be under the /bowling-centers group");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();

    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no bowling centers exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoBowlingCentersExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListBowlingCentersQuery, IReadOnlyCollection<BowlingCenterSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListBowlingCentersQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListBowlingCentersEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}