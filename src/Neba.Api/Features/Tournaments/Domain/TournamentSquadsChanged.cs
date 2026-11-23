using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>Raised when a squad is added to, changed on or removed from a tournament.</summary>
public sealed record TournamentSquadsChanged
    : IDomainEvent
{
    /// <summary>The tournament whose squads changed.</summary>
    public required TournamentId TournamentId { get; init; }
}