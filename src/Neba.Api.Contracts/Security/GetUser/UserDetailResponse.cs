using Neba.Api.Contracts.Compliance;

namespace Neba.Api.Contracts.Security.GetUser;

/// <summary>A user account with the permission overrides set on it.</summary>
public sealed record UserDetailResponse
{
    /// <summary>The user's ID (ULID).</summary>
    public required string UserId { get; init; }

    /// <summary>The user's email address.</summary>
    [PersonalData]
    public required string Email { get; init; }

    /// <summary>Whether the user has confirmed their email (set their password).</summary>
    public required bool EmailConfirmed { get; init; }

    /// <summary>The roles assigned to the user.</summary>
    public required IReadOnlyCollection<string> Roles { get; init; }

    /// <summary>Permission values granted to the user regardless of role.</summary>
    public required IReadOnlyCollection<string> AddedPermissions { get; init; }

    /// <summary>Permission values withheld from the user even when a role grants them.</summary>
    public required IReadOnlyCollection<string> RemovedPermissions { get; init; }
}