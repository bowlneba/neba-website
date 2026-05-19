using StronglyTypedIds;

namespace Neba.Api.Features.HallOfFame.Domain;

/// <summary>
/// Unique identifier for a Hall of Fame induction entry.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct HallOfFameId { }
