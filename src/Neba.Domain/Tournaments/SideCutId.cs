using StronglyTypedIds;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Unique identifier for a side cut.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SideCutId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private SideCutId(Ulid value)
        => Value = value;

    /// <summary>Creates a new <see cref="SideCutId"/> with a randomly generated ULID value.</summary>
    public static SideCutId New()
        => new(Ulid.NewUlid());
}