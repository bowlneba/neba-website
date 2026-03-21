using StronglyTypedIds;

namespace Neba.Domain.Awards;

/// <summary>
/// Unique identifier for a season.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonAwardId;