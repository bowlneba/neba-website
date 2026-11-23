using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>Raised when a tournament is cancelled, or when its start date, end date or type changes.</summary>
public sealed record TournamentChanged
    : IDomainEvent
{
    /// <summary>The tournament that changed.</summary>
    public required TournamentId TournamentId { get; init; }
}