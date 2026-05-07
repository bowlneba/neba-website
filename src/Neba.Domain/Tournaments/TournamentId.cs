using StronglyTypedIds;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Unique identifier for a tournament.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct TournamentId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private TournamentId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="TournamentId"/> with a randomly generated ULID value.
    /// </summary>
    public static TournamentId New()
        => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(TournamentId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TournamentId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(TournamentId a, TournamentId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(TournamentId a, TournamentId b) => !(a == b);
}
