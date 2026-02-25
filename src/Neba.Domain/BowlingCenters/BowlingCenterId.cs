using StronglyTypedIds;

namespace Neba.Domain.BowlingCenters;

/// <summary>
/// Unique identifier for a bowler.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct BowlingCenterId;