using System.Security.Claims;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Infrastructure;

internal static class SecurityRoleSeeder
{
    public const string PermissionClaimType = Permissions.ClaimType;
    private static readonly Dictionary<string, IReadOnlyCollection<Permissions>> RolePermissions = new()
    {
        [Roles.Admin] = Permissions.List
    };

    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var (roleName, permissions) in RolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                role = new ApplicationRole(roleName);
                await roleManager.CreateAsync(role);
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var existingPermissionKeys = existingClaims
                .Where(claim => claim.Type == PermissionClaimType)
                .Select(claim => claim.Value)
                .ToHashSet();

            foreach (var permission in permissions.Where(p => !existingPermissionKeys.Contains(p.Value)))
            {
                await roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, permission.Value));
            }

            var permissionsToRemove = existingClaims
                .Where(claim => claim.Type == PermissionClaimType && !permissions.Any(p => p.Value == claim.Value))
                .ToList();

            foreach (var claim in permissionsToRemove)
            {
                await roleManager.RemoveClaimAsync(role, claim);
            }
        }
    }
}