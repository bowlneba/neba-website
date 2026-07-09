using System.Security.Claims;

namespace Neba.Api.Contracts.Security;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to check for permissions.
/// </summary>
public static class ClaimsPrincipalPermissionExtensions
{
    extension(ClaimsPrincipal user)
    {
        /// <summary>
        /// Checks if the user has any of the specified permissions.
        /// </summary>
        /// <param name="permissions">The permissions to check.</param>
        /// <returns>True if the user has any of the specified permissions; otherwise, false.</returns>
        public bool HasAnyPermission(IReadOnlyCollection<Permissions> permissions)
            => permissions.Any(permission => user.HasClaim(Permissions.ClaimType, permission.Value));
    }
}