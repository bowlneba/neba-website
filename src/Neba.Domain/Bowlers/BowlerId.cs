using StronglyTypedIds;

namespace Neba.Domain.Bowlers;

/// <summary>
/// Unique identifier for a bowler.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct BowlerId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private BowlerId(Ulid value)
        => Value = value;

    /// <summary>Creates a new <see cref="BowlerId"/> with a randomly generated ULID value.</summary>
    public static BowlerId New() => new(Ulid.NewUlid());
}