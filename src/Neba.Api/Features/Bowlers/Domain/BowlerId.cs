using StronglyTypedIds;

namespace Neba.Api.Features.Bowlers.Domain;

/// <summary>
/// Unique identifier for a bowler.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct BowlerId { }
