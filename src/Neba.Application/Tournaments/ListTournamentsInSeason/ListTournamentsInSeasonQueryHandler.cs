using Neba.Application.Messaging;

namespace Neba.Application.Tournaments.ListTournamentsInSeason;

internal sealed class ListTournamentsInSeasonQueryHandler(ITournamentQueries tournamentQueries)
        : IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>
{
    private readonly ITournamentQueries _tournamentQueries = tournamentQueries;

    public async Task<IReadOnlyCollection<SeasonTournamentDto>> HandleAsync(ListTournamentsInSeasonQuery query, CancellationToken cancellationToken)
    {
        var tournaments = await _tournamentQueries.GetTournamentsInSeasonAsync(query.SeasonId, cancellationToken);

        return tournaments;
    }
}