namespace Neba.Api.Contracts.Security.ListUsers;

/// <summary>
/// Summary of a user account for display in an admin user list.
/// </summary>
public sealed record UserSummaryResponse
{
    /// <summary>The user's ID (ULID).</summary>
    public required string UserId { get; init; }

    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>Whether the user has confirmed their email (set their password).</summary>
    public required bool EmailConfirmed { get; init; }

    /// <summary>The roles assigned to the user.</summary>
    public required IReadOnlyCollection<string> Roles { get; init; }
}