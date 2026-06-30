namespace Neba.Api.Contracts.Security.Register;

/// <summary>
/// The fields required to register a new user account.
/// </summary>
public sealed record RegisterInput
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
