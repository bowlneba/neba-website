namespace Neba.Api.Contracts.Security.RefreshToken;

/// <summary>
/// Represents a request to refresh an access token using a refresh token.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the user ID associated with the refresh token.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets or sets the refresh token used to obtain a new access token.
    /// </summary>
    public required string RefreshToken { get; init; }
}