namespace Neba.Api.Contracts.Security.Login;

/// <summary>
/// Represents a request to log in a user with their email and password.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Gets the email address of the user attempting to log in.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the password of the user attempting to log in.
    /// </summary>
    public required string Password { get; init; }
}