using System.Security.Claims;

using ErrorOr;

using Neba.Api.Contracts.Security;

namespace Neba.Api.Security.Domain;

/// <summary>
/// A user's permission overrides: values granted regardless of role (Added) and values withheld even
/// when a role grants them (Removed). Removed wins over every other source.
/// </summary>
internal sealed class UserPermissionOverrides
{
    public static readonly UserPermissionOverrides None = new(new HashSet<string>(), new HashSet<string>());

    private UserPermissionOverrides(IReadOnlySet<string> added, IReadOnlySet<string> removed)
    {
        Added = added;
        Removed = removed;
    }

    public IReadOnlySet<string> Added { get; }

    public IReadOnlySet<string> Removed { get; }

    /// <summary>Builds overrides from caller input, rejecting unknown permissions and any value in both sets.</summary>
    internal static ErrorOr<UserPermissionOverrides> Create(IEnumerable<string> added, IEnumerable<string> removed)
    {
        var addedSet = added.ToHashSet(StringComparer.Ordinal);
        var removedSet = removed.ToHashSet(StringComparer.Ordinal);

        var unknown = addedSet.Concat(removedSet)
            .Where(value => !Permissions.TryFromValue(value, out _))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (unknown.Count > 0)
        {
            return Error.Validation(
                "UserPermissionOverrides.UnknownPermission",
                $"Unknown permission(s): {string.Join(", ", unknown)}.");
        }

        var both = addedSet.Intersect(removedSet).Order(StringComparer.Ordinal).ToList();

        if (both.Count > 0)
        {
            return Error.Validation(
                "UserPermissionOverrides.AddedAndRemoved",
                $"A permission can't be both added and removed: {string.Join(", ", both)}.");
        }

        return new UserPermissionOverrides(addedSet, removedSet);
    }

    /// <summary>
    /// Builds overrides from stored claims. Lenient on purpose: a retired permission value is skipped and
    /// a value stored as both Added and Removed counts as Removed, so bad stored data never breaks login.
    /// </summary>
    internal static UserPermissionOverrides FromStoredClaims(IEnumerable<Claim> claims)
    {
        var claimList = claims.ToList();

        var removed = StoredValues(claimList, Permissions.DeniedClaimType);
        var added = StoredValues(claimList, Permissions.ClaimType);
        added.ExceptWith(removed);

        return new UserPermissionOverrides(added, removed);
    }

    /// <summary>Returns the effective permission values: <c>(role ∪ added) − removed</c>.</summary>
    internal IReadOnlySet<string> ApplyTo(IReadOnlySet<string> rolePermissions)
    {
        var effective = new HashSet<string>(rolePermissions, StringComparer.Ordinal);
        effective.UnionWith(Added);
        effective.ExceptWith(Removed);
        return effective;
    }

    /// <summary>
    /// Returns the permissions this set moves toward more access compared with <paramref name="previous"/>:
    /// newly Added values and lifted Removed values.
    /// </summary>
    internal IReadOnlySet<string> GainsOver(UserPermissionOverrides previous)
    {
        var gains = new HashSet<string>(Added, StringComparer.Ordinal);
        gains.ExceptWith(previous.Added);
        gains.UnionWith(previous.Removed.Where(value => !Removed.Contains(value)));
        return gains;
    }

    private static HashSet<string> StoredValues(IEnumerable<Claim> claims, string claimType)
        => claims
            .Where(claim => claim.Type == claimType && Permissions.TryFromValue(claim.Value, out _))
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);
}