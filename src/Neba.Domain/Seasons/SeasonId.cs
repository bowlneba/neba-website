using StronglyTypedIds;

namespace Neba.Domain.Seasons;

/// <summary>
/// Unique identifier for a season.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private SeasonId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="SeasonId"/> with a randomly generated ULID value.
    /// </summary>
    public static SeasonId New() => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(SeasonId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SeasonId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(SeasonId a, SeasonId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(SeasonId a, SeasonId b) => !(a == b);
}
