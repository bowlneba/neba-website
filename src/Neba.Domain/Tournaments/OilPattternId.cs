using StronglyTypedIds;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Unique identifier for an oil pattern.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct OilPatternId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private OilPatternId(Ulid value)
        => Value = value;

    /// <summary>Creates a new <see cref="OilPatternId"/> with a randomly generated ULID value.</summary>
    public static OilPatternId New()
        => new(Ulid.NewUlid());
}