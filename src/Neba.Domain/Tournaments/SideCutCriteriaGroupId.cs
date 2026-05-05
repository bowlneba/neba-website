using StronglyTypedIds;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Unique identifier for a side cut criteria group.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SideCutCriteriaGroupId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private SideCutCriteriaGroupId(Ulid value)
        => Value = value;

    /// <summary>Creates a new <see cref="SideCutCriteriaGroupId"/> with a randomly generated ULID value.</summary>
    public static SideCutCriteriaGroupId New()
        => new(Ulid.NewUlid());
}
