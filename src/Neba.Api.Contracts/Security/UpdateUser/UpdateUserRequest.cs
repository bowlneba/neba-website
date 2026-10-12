namespace Neba.Api.Contracts.Security.UpdateUser;

/// <summary>Updates an existing user account.</summary>
public sealed record UpdateUserRequest
{
    /// <summary>The ID (ULID string) of the user to update.</summary>
    public required string UserId { get; init; }

    /// <summary>The fields to update.</summary>
    public required UpdateUserInput User { get; init; }
}
