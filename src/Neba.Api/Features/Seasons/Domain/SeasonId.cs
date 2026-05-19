using StronglyTypedIds;

namespace Neba.Api.Features.Seasons.Domain;

/// <summary>
/// Unique identifier for a season.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeasonId { }