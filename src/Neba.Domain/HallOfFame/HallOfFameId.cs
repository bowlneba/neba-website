using StronglyTypedIds;

namespace Neba.Domain.HallOfFame;

/// <summary>
/// Unique identifier for a Hall of Fame induction entry.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct HallOfFameId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private HallOfFameId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="HallOfFameId"/> with a randomly generated ULID value.
    /// </summary>
    public static HallOfFameId New() => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(HallOfFameId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is HallOfFameId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(HallOfFameId a, HallOfFameId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(HallOfFameId a, HallOfFameId b) => !(a == b);
}