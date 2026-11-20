using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>Raised after a squad's scores were replaced by the legacy score sync. Published by the job, not an aggregate.</summary>
public sealed record SquadScoresReplaced
    : IDomainEvent
{
    /// <summary>The tournament the squad belongs to.</summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>The squad whose scores changed.</summary>
    public required SquadId SquadId { get; init; }
}