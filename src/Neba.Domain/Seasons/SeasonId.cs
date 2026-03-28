using StronglyTypedIds;

namespace Neba.Domain.Seasons;

/// <summary>
/// Unique identifier for a season.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonId
{
    /// <summary>Gets the underlying <see cref="Ulid"/> value.</summary>
    public Ulid Value { get; }
    private SeasonId(Ulid value)
        => Value = value;

    /// <summary>Creates a new <see cref="SeasonId"/> with a randomly generated ULID value.</summary>
    public static SeasonId New() => new(Ulid.NewUlid());
}