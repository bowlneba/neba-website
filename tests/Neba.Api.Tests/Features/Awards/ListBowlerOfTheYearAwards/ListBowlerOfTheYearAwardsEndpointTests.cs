using FastEndpoints;

using Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;
using Neba.Application.Awards.ListBowlerOfTheYearAwards;
using Neba.Application.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Awards;

namespace Neba.Api.Tests.Features.Awards.ListBowlerOfTheYearAwards;

[UnitTest]
[Component("Awards")]
public sealed class ListBowlerOfTheYearAwardsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped awards when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedAwards_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = BowlerOfTheYearAwardDtoFactory.Bogus(3, 42);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListBowlerOfTheYearAwardsQuery, IReadOnlyCollection<BowlerOfTheYearAwardDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListBowlerOfTheYearAwardsQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListBowlerOfTheYearAwardsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.HttpContext.Response.ContentType.ShouldNotBeNull();
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(dtos.Count);

        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /awards/bowler-of-the-year")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListBowlerOfTheYearAwardsQuery, IReadOnlyCollection<BowlerOfTheYearAwardDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListBowlerOfTheYearAwardsEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/awards/bowler-of-the-year");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no awards exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoAwardsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListBowlerOfTheYearAwardsQuery, IReadOnlyCollection<BowlerOfTheYearAwardDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListBowlerOfTheYearAwardsQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListBowlerOfTheYearAwardsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}