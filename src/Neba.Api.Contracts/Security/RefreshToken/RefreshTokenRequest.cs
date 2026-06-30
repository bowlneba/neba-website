namespace Neba.Api.Contracts.Security.RefreshToken;

/// <summary>
/// Represents a request to refresh an access token using a refresh token.
/// </summary>
public sealed record RefreshTokenRequest
{
    /// <summary>
    /// Gets the user ID associated with the refresh token.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the refresh token used to obtain a new access token.
    /// </summary>
    public required string RefreshToken { get; init; }
}