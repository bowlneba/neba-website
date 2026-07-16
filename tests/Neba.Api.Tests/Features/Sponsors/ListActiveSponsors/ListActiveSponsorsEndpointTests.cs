using System.Security.Claims;

using FastEndpoints;

using Neba.Api.Contracts.Security;
using Neba.Api.Features.Sponsors.ListActiveSponsors;
using Neba.Api.Messaging;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Sponsors;

namespace Neba.Api.Tests.Features.Sponsors.ListActiveSponsors;

[UnitTest]
[Component("Sponsors")]
public sealed class ListActiveSponsorsEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped sponsors when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMappedSponsors_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = SponsorSummaryDtoFactory.Bogus(3, 42);

        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListActiveSponsorsQuery>(),
                cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<ListActiveSponsorsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "Configure should register anonymous GET route at /sponsors")]
    public void Configure_ShouldRegisterAnonymousGetRoute_AtExpectedPath()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<ListActiveSponsorsEndpoint>(queryHandlerMock.Object);

        // Assert — route and auth
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("sponsors"), "should be under the /sponsors path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no active sponsors exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmptyCollection_WhenNoActiveSponsorsExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(
                It.IsAny<ListActiveSponsorsQuery>(),
                cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListActiveSponsorsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.TotalItems.ShouldBe(0);
        endpoint.Response.Items.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync should set CallerHasSponsorManagementPermission true when user has a sponsor management permission")]
    public async Task HandleAsync_ShouldSetCallerHasSponsorManagementPermissionTrue_WhenUserHasSponsorManagementPermission()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ListActiveSponsorsQuery? capturedQuery = null;

        var queryHandlerMock = new Mock<IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListActiveSponsorsQuery>(), cancellationToken))
            .Callback<ListActiveSponsorsQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListActiveSponsorsEndpoint>(queryHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(Permissions.ClaimType, Permissions.CreateSponsor.Value),
        ]));

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.CallerHasSponsorManagementPermission.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should set CallerHasSponsorManagementPermission false when user has no sponsor management permission")]
    public async Task HandleAsync_ShouldSetCallerHasSponsorManagementPermissionFalse_WhenUserHasNoSponsorManagementPermission()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        ListActiveSponsorsQuery? capturedQuery = null;

        var queryHandlerMock = new Mock<IQueryHandler<ListActiveSponsorsQuery, IReadOnlyCollection<SponsorSummaryDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListActiveSponsorsQuery>(), cancellationToken))
            .Callback<ListActiveSponsorsQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync([]);

        var endpoint = Factory.Create<ListActiveSponsorsEndpoint>(queryHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        capturedQuery.ShouldNotBeNull();
        capturedQuery.CallerHasSponsorManagementPermission.ShouldBeFalse();
    }
}
