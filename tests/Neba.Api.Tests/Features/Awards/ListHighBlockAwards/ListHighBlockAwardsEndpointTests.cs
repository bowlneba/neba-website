using FastEndpoints;

using Neba.Api.Features.Awards.ListHighBlockAwards;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;

namespace Neba.Api.Tests.Features.Awards.ListHighBlockAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListHighBlockAwardsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped awards when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedAwards_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = HighBlockAwardDtoFactory.Bogus(3, 57);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHighBlockAwardsQuery, IReadOnlyCollection<HighBlockAwardDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHighBlockAwardsQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListHighBlockAwardsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.HttpContext.Response.ContentType.ShouldNotBeNull();
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(dtos.Count);

        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /awards/high-block")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListHighBlockAwardsQuery, IReadOnlyCollection<HighBlockAwardDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListHighBlockAwardsEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/awards/high-block");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no awards exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoAwardsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListHighBlockAwardsQuery, IReadOnlyCollection<HighBlockAwardDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListHighBlockAwardsQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListHighBlockAwardsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}