namespace Neba.Api.Security.Domain;

/// <summary>
/// The hashed refresh token record for a user: a small set of independently-valid slots plus the
/// time of the most recent successful redemption (drives absolute expiry).
/// </summary>
public sealed record StoredRefreshToken
{
    /// <summary>Gets the set of hashes currently acceptable for a refresh.</summary>
    public required IReadOnlyList<TokenSlot> Slots { get; init; }

    /// <summary>Gets the time of the most recent successful redemption.</summary>
    public required DateTimeOffset IssuedAt { get; init; }
}

/// <summary>
/// A single hashed refresh token that a refresh call may present.
/// </summary>
public sealed record TokenSlot
{
    /// <summary>Gets the hash of the refresh token this slot accepts.</summary>
    public required string Hash { get; init; }

    /// <summary>
    /// Gets the time at which this slot stops being accepted, or <see langword="null"/> if the slot
    /// is fully valid (not just a short-lived grace window after being superseded). A grace slot
    /// exists so a request that races in presenting a just-superseded hash - one already redeemed by
    /// someone else, or already turned into a new slot itself - isn't rejected outright; redeeming a
    /// grace slot always produces a brand new, fully valid slot, so a racer's reissued token is never
    /// itself a dead end.
    /// </summary>
    public DateTimeOffset? GracedUntil { get; init; }
}
