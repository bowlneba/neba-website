using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Infrastructure.Authorization;

internal static class PermissionResolver
{
    public static async Task<IReadOnlyCollection<Permissions>> ResolveAsync(RoleManager<ApplicationRole> roleManager, IEnumerable<string> roleNames)
    {
        var permissionKeys = new HashSet<string>();

        foreach (var roleName in roleNames)
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

        return [.. permissionKeys.Select(key => Permissions.FromValue(key))];
    }
}