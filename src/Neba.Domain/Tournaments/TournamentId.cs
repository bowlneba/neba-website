using StronglyTypedIds;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Unique identifier for a tournament.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct TournamentId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private TournamentId(Ulid value)
        => Value = value;

    /// <summary>Creates a new <see cref="TournamentId"/> with a randomly generated ULID value.</summary>
    public static TournamentId New()
        => new(Ulid.NewUlid());
}