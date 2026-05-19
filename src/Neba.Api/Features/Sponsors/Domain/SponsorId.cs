using StronglyTypedIds;

namespace Neba.Api.Features.Sponsors.Domain;

/// <summary>
/// Unique identifier for a sponsor.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SponsorId { }