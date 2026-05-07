using StronglyTypedIds;

namespace Neba.Domain.Tournaments;

/// <summary>
/// Unique identifier for an oil pattern.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct OilPatternId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private OilPatternId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="OilPatternId"/> with a randomly generated ULID value.
    /// </summary>
    public static OilPatternId New()
        => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(OilPatternId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is OilPatternId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(OilPatternId a, OilPatternId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(OilPatternId a, OilPatternId b) => !(a == b);
}
