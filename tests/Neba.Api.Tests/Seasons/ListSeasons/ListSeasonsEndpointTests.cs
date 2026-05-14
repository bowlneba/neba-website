using FastEndpoints;

using Neba.Api.Features.Seasons.ListSeasons;
using Neba.Application.Messaging;
using Neba.Application.Seasons;
using Neba.Application.Seasons.ListSeasons;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;

namespace Neba.Api.Tests.Seasons.ListSeasons;

[UnitTest]
[Component("Seasons")]
public sealed class ListSeasonsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped seasons when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedSeasons_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = SeasonDtoFactory.Bogus(3, 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListSeasonsQuery>(), cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListSeasonsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.HttpContext.Response.ContentType.ShouldNotBeNull();
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(dtos.Count);

        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /seasons")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListSeasonsEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain("/seasons/");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no seasons exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoSeasonsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListSeasonsQuery, IReadOnlyCollection<SeasonDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ListSeasonsQuery>(), cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListSeasonsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}