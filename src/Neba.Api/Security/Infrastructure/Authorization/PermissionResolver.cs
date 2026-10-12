using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Infrastructure.Authorization;

internal static class PermissionResolver
{
    public static async Task<IReadOnlyCollection<Permissions>> ResolveAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        IEnumerable<string> roleNames)
    {
        var roles = roleNames.ToList();
        var permissionKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                continue;
            }

            var claims = await roleManager.GetClaimsAsync(role);
            foreach (var claim in claims.Where(c => c.Type == SecurityRoleSeeder.PermissionClaimType))
            {
                permissionKeys.Add(claim.Value);
            }
        }

        // Admin holds every permission by definition. Stored overrides never narrow it, so a user
        // promoted to Admin later can't carry a stale denial. The claim read below is not wrapped in
        // try/catch: a failed read fails the login rather than silently falling back to role-only.
        if (roles.Contains(Roles.Admin, StringComparer.Ordinal))
        {
            return [.. permissionKeys.Select(key => Permissions.FromValue(key))];
        }

        var overrides = UserPermissionOverrides.FromStoredClaims(await userManager.GetClaimsAsync(user));

        return [.. overrides.ApplyTo(permissionKeys).Select(key => Permissions.FromValue(key))];
    }
}