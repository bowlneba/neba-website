using FastEndpoints;

using Neba.Api.Features.Tournaments.ListTournamentTypes;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentTypes;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentTypesEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped tournament types when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedTournamentTypes_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = TournamentTypeSummaryDtoFactory.Bogus(3, 28);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListTournamentTypesQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListTournamentTypesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /tournaments/types")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListTournamentTypesEndpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("tournaments/types"), "should be under the /tournaments/types path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no tournament types exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoTournamentTypesExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListTournamentTypesQuery, IReadOnlyCollection<TournamentTypeSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListTournamentTypesQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListTournamentTypesEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }
}
