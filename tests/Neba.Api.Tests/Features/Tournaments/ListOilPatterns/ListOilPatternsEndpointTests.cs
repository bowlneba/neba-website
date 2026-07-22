using FastEndpoints;

using Neba.Api.Features.Tournaments.ListOilPatterns;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListOilPatterns;

[UnitTest]
[Component("Tournaments")]
public sealed class ListOilPatternsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped oil patterns when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedOilPatterns_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = OilPatternSummaryDtoFactory.Bogus(3, 28);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListOilPatternsQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListOilPatternsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /oil-patterns")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListOilPatternsEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("oil-patterns"), "should be under the /oil-patterns group");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no oil patterns exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoOilPatternsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListOilPatternsQuery, IReadOnlyCollection<OilPatternSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListOilPatternsQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListOilPatternsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}
