namespace Neba.Api.Contracts.Security.GetUser;

/// <summary>Identifies the user to return.</summary>
public sealed record GetUserRequest
{
    /// <summary>The user's ID (ULID string).</summary>
    public required string UserId { get; init; }
}
