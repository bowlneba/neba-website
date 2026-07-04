using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using Neba.Api.Identity;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Identity;

[UnitTest]
[Component("Identity")]
public sealed class CurrentUserServiceTests
{
    private static CurrentUserService CreateService(HttpContext? ctx)
    {
        var accessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        accessor.SetupGet(a => a.HttpContext).Returns(ctx);
        return new CurrentUserService(accessor.Object);
    }

    private static DefaultHttpContext AuthenticatedContext(string userId = "user-123")
    {
        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                authenticationType: "TestAuth"))
        };
    }

    // ── ActorId ────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "ActorId returns NameIdentifier claim when HttpContext and claim are present")]
    public void ActorId_WhenNameIdentifierClaimPresent_ReturnsClaimValue()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext("alice"));

        // Act
        var actorId = service.ActorId;

        // Assert
        actorId.ShouldBe("alice");
    }

    [Fact(DisplayName = "ActorId returns 'anonymous' when HttpContext is null")]
    public void ActorId_WhenHttpContextIsNull_ReturnsAnonymous()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var actorId = service.ActorId;

        // Assert
        actorId.ShouldBe("anonymous");
    }

    [Fact(DisplayName = "ActorId returns 'anonymous' when NameIdentifier claim is missing")]
    public void ActorId_WhenNoNameIdentifierClaim_ReturnsAnonymous()
    {
        // Arrange
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var service = CreateService(ctx);

        // Act
        var actorId = service.ActorId;

        // Assert
        actorId.ShouldBe("anonymous");
    }

    // ── IsAuthenticated ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "IsAuthenticated returns true when identity is authenticated")]
    public void IsAuthenticated_WhenIdentityIsAuthenticated_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext());

        // Act
        var isAuthenticated = service.IsAuthenticated;

        // Assert
        isAuthenticated.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsAuthenticated returns false when HttpContext is null")]
    public void IsAuthenticated_WhenHttpContextIsNull_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var isAuthenticated = service.IsAuthenticated;

        // Assert
        isAuthenticated.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsAuthenticated returns false when identity is not authenticated")]
    public void IsAuthenticated_WhenIdentityIsNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var service = CreateService(ctx);

        // Act
        var isAuthenticated = service.IsAuthenticated;

        // Assert
        isAuthenticated.ShouldBeFalse();
    }
}
