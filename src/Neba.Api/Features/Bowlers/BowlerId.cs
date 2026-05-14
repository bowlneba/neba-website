using StronglyTypedIds;

namespace Neba.Api.Features.Bowlers;

/// <summary>
/// Unique identifier for a bowler.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct BowlerId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private BowlerId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="BowlerId"/> with a randomly generated ULID value.
    /// </summary>
    public static BowlerId New() => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(BowlerId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is BowlerId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(BowlerId a, BowlerId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(BowlerId a, BowlerId b) => !(a == b);
}