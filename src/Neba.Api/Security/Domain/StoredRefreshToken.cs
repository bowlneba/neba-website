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

    /// <summary>
    /// Gets the hash of the immediately-prior refresh token, kept for a short grace window after
    /// rotation so a refresh call already in flight when another call rotates the token first
    /// doesn't get rejected. Null once the grace window has passed or for a token that hasn't
    /// rotated yet.
    /// </summary>
    public string? PreviousHash { get; init; }

    /// <summary>Gets the time at which <see cref="PreviousHash"/> stops being accepted.</summary>
    public DateTimeOffset? PreviousHashExpiresAt { get; init; }
}