namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>Response returned after a successful staff user creation. Contains the new user's unique identifier.</summary>
public sealed record CreateUserResponse
{
    /// <summary>
    /// The unique identifier of the newly created user.
    /// </summary>
    public required string UserId { get; init; }
}