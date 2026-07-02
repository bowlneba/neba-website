using Microsoft.AspNetCore.Authorization;

namespace Neba.Api.Contracts.Security.Authorization;

/// <summary>
/// Succeeds a <see cref="PermissionRequirement"/> when the current user has a matching
/// <see cref="Permissions.ClaimType"/> claim.
/// </summary>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.HasClaim(Permissions.ClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}