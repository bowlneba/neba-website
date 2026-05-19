using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for an oil pattern.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct OilPatternId { }