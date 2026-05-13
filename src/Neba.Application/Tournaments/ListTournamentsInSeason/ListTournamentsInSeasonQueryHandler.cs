using Neba.Application.Messaging;
using Neba.Application.Storage;

namespace Neba.Application.Tournaments.ListTournamentsInSeason;

internal sealed class ListTournamentsInSeasonQueryHandler(
    ITournamentQueries tournamentQueries,
    IFileStorageService fileStorageService)
    : IQueryHandler<ListTournamentsInSeasonQuery, IReadOnlyCollection<SeasonTournamentDto>>
{
    private readonly ITournamentQueries _tournamentQueries = tournamentQueries;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<IReadOnlyCollection<SeasonTournamentDto>> HandleAsync(ListTournamentsInSeasonQuery query, CancellationToken cancellationToken)
    {
        var tournaments = await _tournamentQueries.GetTournamentsInSeasonAsync(query.SeasonId, cancellationToken);

        return [.. tournaments.Select(t =>
        {
            if (t.LogoContainer is not null && t.LogoPath is not null)
                t = t with { LogoUrl = _fileStorageService.GetBlobUri(t.LogoContainer, t.LogoPath) };

            var sponsors = t.Sponsors
                .Select(s => s.LogoContainer is not null && s.LogoPath is not null
                    ? s with { LogoUrl = _fileStorageService.GetBlobUri(s.LogoContainer, s.LogoPath) }
                    : s)
                .ToArray();

            return t with { Sponsors = sponsors };
        })];
    }
}