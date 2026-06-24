namespace Neba.Api.Contracts.Security.Register;

/// <summary>
/// Registers a new user account. Day 1: admin-created only; self-registration is Phase 8.
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>
    /// The new user's email address. Used as both username and login identifier.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The initial password. Must meet the API's password policy (8+ chars, at least one digit).
    /// </summary>
    public required string Password { get; init; }
}