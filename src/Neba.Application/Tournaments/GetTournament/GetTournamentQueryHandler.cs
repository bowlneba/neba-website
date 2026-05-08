using ErrorOr;

using Neba.Application.Messaging;

namespace Neba.Application.Tournaments.GetTournament;

internal sealed class GetTournamentQueryHandler(ITournamentQueries tournamentQueries)
    : IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>
{
    private readonly ITournamentQueries _tournamentQueries = tournamentQueries;

    public async Task<ErrorOr<TournamentDetailDto>> HandleAsync(GetTournamentQuery query, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentQueries.GetTournamentDetailAsync(query.Id, cancellationToken);

        return tournament is null
            ? TournamentErrors.TournamentNotFound(query.Id)
            : tournament;
    }
}