using StronglyTypedIds;

namespace Neba.Domain.Seasons;

/// <summary>
/// Unique identifier for a season award.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonAwardId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private SeasonAwardId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="SeasonAwardId"/> with a randomly generated ULID value.
    /// </summary>
    public static SeasonAwardId New() => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(SeasonAwardId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SeasonAwardId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(SeasonAwardId a, SeasonAwardId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(SeasonAwardId a, SeasonAwardId b) => !(a == b);
}