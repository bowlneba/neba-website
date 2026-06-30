namespace Neba.Api.Contracts.Security.Login;

/// <summary>
/// Represents a response returned after a successful login attempt, containing authentication tokens and user information.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>
    /// Gets the access token issued to the user upon successful authentication.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the refresh token issued to the user, which can be used to obtain a new access token when the current one expires.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the expiration time of the access token, indicating when the token will no longer be valid.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user who has successfully logged in.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the email address of the user who has successfully logged in.
    /// </summary>
    public required string Email { get; init; }
}