namespace Neba.Api.Contracts.Security.RefreshToken;

/// <summary>
/// Represents a response containing a new access token and refresh token after a successful refresh operation.
/// </summary>
public sealed record RefreshTokenResponse
{
    /// <summary>
    /// Gets or sets the new access token issued after a successful refresh operation.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets or sets the new refresh token issued after a successful refresh operation.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets or sets the expiration time of the new access token in ISO 8601 format.
    /// </summary>
    public required string ExpiresAt { get; init; }

    /// <summary>
    /// Gets or sets the user ID associated with the refreshed tokens.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets or sets the email address of the user associated with the refreshed tokens.
    /// </summary>
    public required string Email { get; init; }
}