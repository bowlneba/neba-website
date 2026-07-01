using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

using Neba.Api.Security.Infrastructure;
using Neba.Api.Security.Infrastructure.Authorization;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Security.Infrastructure.Authorization;

[UnitTest]
[Component("Security")]
public sealed class PermissionAuthorizationHandlerTests
{
    private static AuthorizationHandlerContext CreateContext(PermissionRequirement requirement, ClaimsPrincipal user)
        => new([requirement], user, resource: null);

    [Fact(DisplayName = "HandleAsync should succeed when the user has the required permission claim")]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasRequiredPermissionClaim()
    {
        // Arrange
        var requirement = new PermissionRequirement("Read");
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(SecurityRoleSeeder.PermissionClaimType, "Read"),
        ]));
        var context = CreateContext(requirement, user);
        var handler = new PermissionAuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact(DisplayName = "HandleAsync should not succeed when the user does not have the required permission claim")]
    public async Task HandleAsync_ShouldNotSucceed_WhenUserDoesNotHaveRequiredPermissionClaim()
    {
        // Arrange
        var requirement = new PermissionRequirement("Read");
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(SecurityRoleSeeder.PermissionClaimType, "Write"),
        ]));
        var context = CreateContext(requirement, user);
        var handler = new PermissionAuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact(DisplayName = "HandleAsync should not succeed when the user has no claims")]
    public async Task HandleAsync_ShouldNotSucceed_WhenUserHasNoClaims()
    {
        // Arrange
        var requirement = new PermissionRequirement("Read");
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(requirement, user);
        var handler = new PermissionAuthorizationHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }
}
