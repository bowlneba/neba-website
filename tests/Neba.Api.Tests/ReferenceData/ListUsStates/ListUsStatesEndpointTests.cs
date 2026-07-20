using FastEndpoints;

using Neba.Api.Messaging;
using Neba.Api.ReferenceData.ListUsStates;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.ReferenceData.ListUsStates;

[UnitTest]
[Component("ReferenceData")]
public sealed class ListUsStatesEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped US states when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedUsStates_WhenQuerySucceeds()
    {
        // Arrange
        IReadOnlyCollection<UsStateDto> dtos =
        [
            new UsStateDto { Name = "Massachusetts", Code = "MA" },
            new UsStateDto { Name = "Connecticut", Code = "CT" }
        ];

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListUsStatesQuery, IReadOnlyCollection<UsStateDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListUsStatesQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListUsStatesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(2);
        endpoint.Response.Items.ShouldContain(r => r.Code == "MA" && r.Name == "Massachusetts");
        endpoint.Response.Items.ShouldContain(r => r.Code == "CT" && r.Name == "Connecticut");
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /reference-data/us-states")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListUsStatesQuery, IReadOnlyCollection<UsStateDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListUsStatesEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("us-states"), "should be under the /reference-data group");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no states are returned")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoStatesReturned()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListUsStatesQuery, IReadOnlyCollection<UsStateDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListUsStatesQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListUsStatesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}
