using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a game score.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SquadScoreId;