using System.Text.RegularExpressions;

namespace Neba.Website.Server.Account.PermissionOverrides;

/// <summary>Rules and display helpers the permission overrides UI shares across pages.</summary>
public static partial class PermissionOverrideRules
{
    /// <summary>
    /// The API's role type is internal to Neba.Api, so the name is repeated here, as in
    /// <see cref="Neba.Website.Server.Account.CreateUser.StaffRoleOptions"/>.
    /// </summary>
    public const string AdminRoleName = "Admin";

    private static readonly string[] AreaOrder = ["System", "News", "Sponsors", "Tournaments"];

    /// <summary>
    /// True when moving from <paramref name="original"/> to <paramref name="next"/> gives the user more
    /// access: a new Added, or lifting a Removed. Only the caller's own permissions can authorize that.
    /// </summary>
    public static bool IsGain(PermissionOverrideState original, PermissionOverrideState next)
        => (next == PermissionOverrideState.Added && original != PermissionOverrideState.Added)
            || (original == PermissionOverrideState.Removed && next != PermissionOverrideState.Removed);

    /// <summary>Whether a permission is allowed for the user given its override state and whether a role grants it.</summary>
    public static bool IsAllowed(PermissionOverrideState state, bool grantedByARole)
        => state switch
        {
            PermissionOverrideState.Added => true,
            PermissionOverrideState.Removed => false,
            _ => grantedByARole
        };

    /// <summary>The area is the permission value's prefix (e.g. "Tournaments" in "Tournaments.EditTournament").</summary>
    public static string AreaOf(string permissionValue)
    {
        ArgumentNullException.ThrowIfNull(permissionValue);

        var dot = permissionValue.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? permissionValue : permissionValue[..dot];
    }

    /// <summary>Known areas first, in a fixed order; the rest alphabetically.</summary>
    public static int AreaSortKey(string area)
    {
        var index = Array.IndexOf(AreaOrder, area);
        return index < 0 ? AreaOrder.Length : index;
    }

    /// <summary>"BackgroundJobs" becomes "Background Jobs".</summary>
    public static string AreaDisplayName(string area)
        => WordBoundary().Replace(area, " ");

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}