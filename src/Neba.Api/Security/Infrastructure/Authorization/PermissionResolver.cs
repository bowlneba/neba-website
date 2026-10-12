using Microsoft.EntityFrameworkCore;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Infrastructure.Authorization;

internal static class PermissionResolver
{
    public static async Task<IReadOnlyCollection<Permissions>> ResolveAsync(
        SecurityDbContext securityDbContext,
        ApplicationUser user,
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken)
    {
        var roles = roleNames.ToList();

        // One query for every role's grants, instead of a role lookup and a claim read per role.
        var roleGrants = await securityDbContext.RoleClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType == SecurityRoleSeeder.PermissionClaimType
                && securityDbContext.Roles.Any(role => role.Id == claim.RoleId && roles.Contains(role.Name!)))
            .Select(claim => claim.ClaimValue!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissionKeys = new HashSet<string>(roleGrants, StringComparer.Ordinal);

        // Admin holds every permission by definition. Stored overrides never narrow it, so a user
        // promoted to Admin later can't carry a stale denial. The claim read below is not wrapped in
        // try/catch: a failed read fails the login rather than silently falling back to role-only.
        if (roles.Contains(Roles.Admin, StringComparer.Ordinal))
        {
            return [.. permissionKeys.Select(key => Permissions.FromValue(key))];
        }

        var storedClaims = await securityDbContext.UserClaims
            .AsNoTracking()
            .Where(claim => claim.UserId == user.Id
                && (claim.ClaimType == Permissions.ClaimType || claim.ClaimType == Permissions.DeniedClaimType))
            .ToListAsync(cancellationToken);

        var overrides = UserPermissionOverrides.FromStoredClaims(storedClaims.Select(claim => claim.ToClaim()));

        return [.. overrides.ApplyTo(permissionKeys).Select(key => Permissions.FromValue(key))];
    }
}
