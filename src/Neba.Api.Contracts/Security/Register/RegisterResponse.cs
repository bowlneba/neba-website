namespace Neba.Api.Contracts.Security.Register;

/// <summary>
/// Response returned after a successful user registration. Contains the new user's unique identifier (UserId).
/// </summary>
public sealed record RegisterResponse
{
    /// <summary>
    /// Gets the unique identifier of the newly registered user. This is typically a GUID or other unique value assigned by the system upon successful registration.
    /// </summary>
    public required string UserId { get; init; }
}