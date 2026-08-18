using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a tournament result.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct TournamentResultId;