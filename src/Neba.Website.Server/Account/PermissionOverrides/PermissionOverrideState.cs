namespace Neba.Website.Server.Account.PermissionOverrides;

/// <summary>How one permission is set for one user.</summary>
public enum PermissionOverrideState
{
    /// <summary>Their roles decide.</summary>
    Default,

    /// <summary>Allowed regardless of roles.</summary>
    Added,

    /// <summary>Blocked regardless of roles.</summary>
    Removed
}
