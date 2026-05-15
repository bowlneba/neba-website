using StronglyTypedIds;

namespace Neba.Api.Features.Sponsors.Domain;

/// <summary>
/// Unique identifier for a sponsor.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SponsorId
{
    /// <summary>
    /// Gets the underlying <see cref="Ulid"/> value.
    /// </summary>
    public Ulid Value { get; }

    private SponsorId(Ulid value)
        => Value = value;

    /// <summary>
    /// Creates a new <see cref="SponsorId"/> with a randomly generated ULID value.
    /// </summary>
    public static SponsorId New() => new(Ulid.NewUlid());

    /// <inheritdoc/>
    public bool Equals(SponsorId other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SponsorId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns <see langword="true"/> if the two values are equal.
    /// </summary>
    public static bool operator ==(SponsorId a, SponsorId b) => a.Equals(b);

    /// <summary>
    /// Returns <see langword="true"/> if the two values are not equal.
    /// </summary>
    public static bool operator !=(SponsorId a, SponsorId b) => !(a == b);
}