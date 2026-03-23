using StronglyTypedIds;

namespace Neba.Domain.Seasons;

/// <summary>
/// Unique identifier for a season.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonId;