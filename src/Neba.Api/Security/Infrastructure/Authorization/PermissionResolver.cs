using System.Security.Claims;

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
        var (granted, _) = await ResolveKeysAsync(securityDbContext, user, roleNames, cancellationToken);

        return [.. granted.Select(key => Permissions.FromValue(key))];
    }

    /// <summary>
    /// The effective permissions plus the ones a stored denial withholds, from one read of the user's
    /// claims. Admin holds every permission by definition, so a stored denial never applies to it.
    /// </summary>
    public static async Task<(IReadOnlyCollection<Permissions> Granted, IReadOnlyCollection<Permissions> Denied)> ResolveWithDeniedAsync(
        SecurityDbContext securityDbContext,
        ApplicationUser user,
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken)
    {
        var (granted, denied) = await ResolveKeysAsync(securityDbContext, user, roleNames, cancellationToken);

        return (
            [.. granted.Select(key => Permissions.FromValue(key))],
            [.. denied.Select(key => Permissions.FromValue(key))]);
    }

    // Denied stays a lazy sequence so ResolveAsync, on the login hot path, never materializes it.
    private static async Task<(IEnumerable<string> Granted, IEnumerable<string> Denied)> ResolveKeysAsync(
        SecurityDbContext securityDbContext,
        ApplicationUser user,
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken)
    {
        var roles = roleNames.ToList();
        var isAdmin = roles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase);

        // Identity's default normalizer upper-cases names, so match on NormalizedName like FindByNameAsync did.
        var normalizedRoles = roles.ConvertAll(role => role.ToUpperInvariant());

        var roleGrants = securityDbContext.RoleClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType == SecurityRoleSeeder.PermissionClaimType
                && securityDbContext.Roles.Any(role => role.Id == claim.RoleId
                    && normalizedRoles.Contains(role.NormalizedName!)))
            .Select(claim => new GrantRow { FromRole = true, Type = claim.ClaimType, Value = claim.ClaimValue });

        // Admin holds every permission by definition. Stored overrides never narrow it, so a user
        // promoted to Admin later can't carry a stale denial. The claim read is not wrapped in
        // try/catch: a failed read fails the login rather than silently falling back to role-only.
        if (isAdmin)
        {
            var adminGrants = await roleGrants.ToListAsync(cancellationToken);

            return (adminGrants.Select(row => row.Value!).Distinct(StringComparer.Ordinal), []);
        }

        // Role grants and the user's stored overrides come back from one round trip.
        var userClaims = securityDbContext.UserClaims
            .AsNoTracking()
            .Where(claim => claim.UserId == user.Id
                && (claim.ClaimType == Permissions.ClaimType || claim.ClaimType == Permissions.DeniedClaimType))
            .Select(claim => new GrantRow { FromRole = false, Type = claim.ClaimType, Value = claim.ClaimValue });

        var rows = await roleGrants.Concat(userClaims).ToListAsync(cancellationToken);

        var permissionKeys = new HashSet<string>(
            rows.Where(row => row.FromRole).Select(row => row.Value!), StringComparer.Ordinal);

        var storedClaims = rows
            .Where(row => !row.FromRole)
            .Select(row => new Claim(row.Type!, row.Value!));

        var overrides = UserPermissionOverrides.FromStoredClaims(storedClaims);

        return (overrides.ApplyTo(permissionKeys), overrides.Removed);
    }

    private sealed class GrantRow
    {
        public bool FromRole { get; init; }

        public string? Type { get; init; }

        public string? Value { get; init; }
    }
}