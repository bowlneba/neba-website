using StronglyTypedIds;

namespace Neba.Domain.Seasons;

/// <summary>
/// Unique identifier for a season.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonId
{
    /// <summary>Creates a new <see cref="SeasonId"/> with a randomly generated ULID value.</summary>
    public static SeasonId New() => new(Ulid.NewUlid(), skipValidation: true);
}