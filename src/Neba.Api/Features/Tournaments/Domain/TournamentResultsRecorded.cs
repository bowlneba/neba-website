using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>Raised after a sync wrote a tournament's results. Fires on every re-sync.</summary>
public sealed record TournamentResultsRecorded
    : IDomainEvent
{
    /// <summary>The tournament whose results were written.</summary>
    public required TournamentId TournamentId { get; init; }
}