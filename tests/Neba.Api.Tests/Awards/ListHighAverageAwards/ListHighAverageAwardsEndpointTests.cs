using FastEndpoints;

using Neba.Api.Awards.ListHighAverageAwards;
using Neba.Application.Awards.ListHighAverageAwards;
using Neba.Application.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;

namespace Neba.Api.Tests.Awards.ListHighAverageAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListHighAverageAwardsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped awards when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedAwards_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = HighAverageAwardDtoFactory.Bogus(3, 73);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHighAverageAwardsQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListHighAverageAwardsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.HttpContext.Response.ContentType.ShouldNotBeNull();
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(dtos.Count);

        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /awards/high-average")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListHighAverageAwardsEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("high-average"), "should be under the /awards/high-average group");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no awards exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoAwardsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHighAverageAwardsQuery, IReadOnlyCollection<HighAverageAwardDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHighAverageAwardsQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListHighAverageAwardsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}
