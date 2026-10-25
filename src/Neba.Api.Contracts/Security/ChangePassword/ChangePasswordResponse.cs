using Neba.Api.Contracts.Compliance;

namespace Neba.Api.Contracts.Security.ChangePassword;

/// <summary>
/// The new token pair issued after a password change.
/// </summary>
public sealed record ChangePasswordResponse
{
    /// <summary>
    /// The new access token.
    /// </summary>
    [PrivateData]
    public required string AccessToken { get; init; }

    /// <summary>
    /// The new refresh token. Every earlier refresh token is revoked.
    /// </summary>
    [PrivateData]
    public required string RefreshToken { get; init; }

    /// <summary>
    /// When the new access token expires.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// The user's ID.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    [PersonalData]
    public required string Email { get; init; }
}
