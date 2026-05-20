using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a tournament.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct TournamentId;