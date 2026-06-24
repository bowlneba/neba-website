namespace Neba.Api.Security.Domain;

/// <summary>
/// A hashed refresh token issued to a user, with the time it was issued.
/// </summary>
public sealed record StoredRefreshToken
{
    /// <summary>Gets the hash of the refresh token.</summary>
    public required string Hash { get; init; }

    /// <summary>Gets the date and time the token was issued.</summary>
    public required DateTimeOffset IssuedAt { get; init; }
}