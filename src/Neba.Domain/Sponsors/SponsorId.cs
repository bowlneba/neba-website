using StronglyTypedIds;

namespace Neba.Domain.Sponsors;

/// <summary>
/// Unique identifier for a sponsor.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SponsorId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private SponsorId(Ulid value)
        => Value = value;

    /// <summary>Creates a new <see cref="SponsorId"/> with a randomly generated ULID value.</summary>
    public static SponsorId New() => new(Ulid.NewUlid());
}