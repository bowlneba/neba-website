using StronglyTypedIds;

namespace Neba.Domain.Seasons;

/// <summary>
/// Unique identifier for a season award.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonAwardId
{
    /// <summary>Creates a new <see cref="SeasonAwardId"/> with a randomly generated ULID value.</summary>
    public static SeasonAwardId New() => new(Ulid.NewUlid(), skipValidation: true);
}