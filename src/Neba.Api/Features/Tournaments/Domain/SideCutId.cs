using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a side cut.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SideCutId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private SideCutId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="SideCutId"/> with a randomly generated ULID value.
    /// </summary>
    public static SideCutId New()
        => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(SideCutId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SideCutId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(SideCutId a, SideCutId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(SideCutId a, SideCutId b) => !(a == b);
}