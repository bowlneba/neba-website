namespace Neba.Api.Security;

/// <summary>
/// Represents the settings for JWT (JSON Web Token) authentication, including issuer, audience, signing key, and token expiration times.
/// </summary>
internal sealed record JwtSettings
{
    /// <summary>
    /// Gets or sets the issuer of the JWT tokens. This is typically the URL of the authentication server or service that issues the tokens.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the audience for the JWT tokens. This is typically the URL of the resource server or service that the tokens are intended for.
    /// </summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the signing key used to sign the JWT tokens. This key is used to verify the authenticity of the tokens and should be kept secret.
    /// </summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time for access tokens in minutes. This defines how long an access token is valid before it expires and needs to be refreshed.
    /// </summary>
    public int AccessTokenExpiryMinutes { get; init; } = 15;

    /// <summary>
    /// Gets or sets the expiration time for refresh tokens in days. This defines how long a refresh token is valid before it expires and needs to be reissued.
    /// </summary>
    public int RefreshTokenExpiryDays { get; init; } = 7;
}