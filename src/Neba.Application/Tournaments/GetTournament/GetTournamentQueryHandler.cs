using ErrorOr;

using Neba.Application.Messaging;
using Neba.Application.Storage;

namespace Neba.Application.Tournaments.GetTournament;

internal sealed class GetTournamentQueryHandler(
    ITournamentQueries tournamentQueries,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>
{
    private readonly ITournamentQueries _tournamentQueries = tournamentQueries;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<ErrorOr<TournamentDetailDto>> HandleAsync(GetTournamentQuery query, CancellationToken cancellationToken)
    {
        var tournament = await _tournamentQueries.GetTournamentDetailAsync(query.Id, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(query.Id);
        }

        if (tournament.LogoContainer is not null && tournament.LogoPath is not null)
        {
            tournament = tournament with { LogoUrl = _fileStorageService.GetBlobUri(tournament.LogoContainer, tournament.LogoPath) };
        }

        var sponsors = tournament.Sponsors
            .Select(s => s.LogoContainer is not null && s.LogoPath is not null
                ? s with { LogoUrl = _fileStorageService.GetBlobUri(s.LogoContainer, s.LogoPath) }
                : s)
            .ToArray();
        tournament = tournament with { Sponsors = sponsors };

        return tournament;
    }
}