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
        [Roles.Admin] = Permissions.List,

        [Roles.Webmaster] =
        [
            Permissions.CreateUser,
            Permissions.ResetUserPassword,

            Permissions.CreateArticle,
            Permissions.EditArticle,
            Permissions.DeleteArticle,

            Permissions.CreateSponsor,
            Permissions.EditSponsor,

            Permissions.CreateTournament,
            Permissions.EditTournament,
            Permissions.ManageTournamentSponsors,
            Permissions.DeleteTournament
        ],

        [Roles.Manager] =
        [
            Permissions.CreateArticle,
            Permissions.EditArticle,
            Permissions.DeleteArticle,

            Permissions.CreateSponsor,
            Permissions.EditSponsor,

            Permissions.CreateTournament,
            Permissions.EditTournament,
            Permissions.ManageTournamentSponsors
        ],

        [Roles.TournamentDirector] =
        [
            Permissions.CreateTournament,
            Permissions.EditTournament,
            Permissions.ManageTournamentSponsors
        ],

        [Roles.Journalist] =
        [
            Permissions.CreateArticle,
            Permissions.EditArticle,
            Permissions.DeleteArticle
        ],

        [Roles.Member] = []
    };

    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        foreach ((string roleName, IReadOnlyCollection<Permissions> permissions) in RolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                role = new ApplicationRole(roleName);
                EnsureSucceeded(await roleManager.CreateAsync(role), logger, roleName, $"create role '{roleName}'");
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var existingPermissionKeys = existingClaims
                .Where(claim => claim.Type == PermissionClaimType)
                .Select(claim => claim.Value)
                .ToHashSet();

            var permissionsToAdd = permissions
                .Select(p => p.Value)
                .Where(value => !existingPermissionKeys.Contains(value))
                .ToList();

            foreach (var permissionValue in permissionsToAdd)
            {
                EnsureSucceeded(
                    await roleManager.AddClaimAsync(role, new Claim(PermissionClaimType, permissionValue)),
                    logger, roleName, $"add permission claim '{permissionValue}'");
            }

            var permissionsToRemove = existingClaims
                .Where(claim => claim.Type == PermissionClaimType && !permissions.Any(p => p.Value == claim.Value))
                .ToList();

            foreach (var claim in permissionsToRemove)
            {
                EnsureSucceeded(
                    await roleManager.RemoveClaimAsync(role, claim),
                    logger, roleName, $"remove permission claim '{claim.Value}'");
            }
        }
    }

    /// <summary>
    /// Role/permission seeding is a startup-critical operation — an unchecked <see cref="IdentityResult"/>
    /// failure here would silently leave a role without its expected permission claims, with no
    /// visible symptom other than users in that role lacking access at runtime. Logging and failing
    /// startup surfaces the problem immediately instead of as a confusing authorization bug later.
    /// </summary>
    private static void EnsureSucceeded(IdentityResult result, ILogger logger, string roleName, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
        logger.LogRoleSeedOperationFailed(roleName, operation, errors);
        throw new InvalidOperationException($"Security role seeding failed to {operation}. Errors: {errors}");
    }
}

internal static partial class SecurityRoleSeederLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "Security role seeding failed for role '{RoleName}': could not {Operation}. Errors: {Errors}")]
    public static partial void LogRoleSeedOperationFailed(
        this ILogger logger, string roleName, string operation, string errors);
}