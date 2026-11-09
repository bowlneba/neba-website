using StronglyTypedIds;

namespace Neba.Api.Features.Memberships.Domain;

/// <summary>
/// Unique identifier for a bowler membership.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct BowlerMembershipId;