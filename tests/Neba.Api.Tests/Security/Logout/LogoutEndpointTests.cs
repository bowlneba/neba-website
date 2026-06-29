using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using ErrorOr;

using FastEndpoints;

using Neba.Api.Security.Logout;
using Neba.TestFactory.Attributes;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security.Logout;

[UnitTest]
[Component("Security")]
public sealed class LogoutEndpointTests
{
    [Fact(DisplayName = "HandleAsync should return 204 NoContent and dispatch command when user ID is in JWT claims")]
    public async Task HandleAsync_ShouldReturn204AndDispatchCommand_WhenUserIdIsInJwtClaims()
    {
        // Arrange
        var userId = Ulid.Parse("01000000000000000000000001", CultureInfo.InvariantCulture);
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<LogoutCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<LogoutCommand>(c => c.UserId == userId),
                ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<LogoutEndpoint>(commandHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        ]));

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        commandHandlerMock.VerifyAll();
    }

    [Fact(DisplayName = "HandleAsync should return 204 NoContent and dispatch command when user ID is in NameIdentifier claim")]
    public async Task HandleAsync_ShouldReturn204AndDispatchCommand_WhenUserIdIsInNameIdentifierClaim()
    {
        // Arrange
        var userId = Ulid.Parse("01000000000000000000000002", CultureInfo.InvariantCulture);
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<LogoutCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(
                It.Is<LogoutCommand>(c => c.UserId == userId),
                ct))
            .ReturnsAsync(Result.Success);

        var endpoint = Factory.Create<LogoutEndpoint>(commandHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        ]));

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        commandHandlerMock.VerifyAll();
    }

    [Fact(DisplayName = "HandleAsync should return 204 NoContent without dispatching command when no user ID claim is present")]
    public async Task HandleAsync_ShouldReturn204WithoutDispatching_WhenNoUserIdClaimIsPresent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<LogoutCommand>>(MockBehavior.Strict);

        var endpoint = Factory.Create<LogoutEndpoint>(commandHandlerMock.Object);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        await endpoint.HandleAsync(ct);

        // Assert
        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        commandHandlerMock.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Configure should register authenticated POST route containing 'logout'")]
    public void Configure_ShouldRegisterAuthenticatedPostRoute_ContainingLogout()
    {
        // Arrange
        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<LogoutCommand>>(MockBehavior.Strict);
        var endpoint = Factory.Create<LogoutEndpoint>(commandHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("POST");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("logout"), "should include a 'logout' route");
        endpoint.Definition.AnonymousVerbs.ShouldBeNull();
    }
}