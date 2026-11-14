namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// The facts about a previous tournament that <see cref="Tournament"/> needs to accept it as its previous tournament.
/// </summary>
public readonly record struct PreviousTournamentFacts(TournamentId Id, TournamentType TournamentType, DateOnly EndDate);
