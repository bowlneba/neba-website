using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class PreviousTournamentFactsFactory
{
    public static PreviousTournamentFacts Create(
        TournamentId? id = null,
        TournamentType? tournamentType = null,
        DateOnly? endDate = null)
        => new(
            id ?? TournamentId.New(),
            tournamentType ?? TournamentFactory.ValidTournamentType,
            endDate ?? new DateOnly(2025, 3, 2));
}