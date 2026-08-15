using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a squad.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SquadId;