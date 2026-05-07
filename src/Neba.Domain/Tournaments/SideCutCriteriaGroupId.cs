using StronglyTypedIds;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Unique identifier for a side cut criteria group.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SideCutCriteriaGroupId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private SideCutCriteriaGroupId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="SideCutCriteriaGroupId"/> with a randomly generated ULID value.
    /// </summary>
    public static SideCutCriteriaGroupId New()
        => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(SideCutCriteriaGroupId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SideCutCriteriaGroupId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(SideCutCriteriaGroupId a, SideCutCriteriaGroupId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(SideCutCriteriaGroupId a, SideCutCriteriaGroupId b) => !(a == b);
}
