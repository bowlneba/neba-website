using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class TournamentQueries(AppDbContext appDbContext)
    : ITournamentQueries
{
    private readonly IQueryable<Tournament> _tournaments
        = appDbContext.Tournaments.AsNoTracking();

    private readonly IQueryable<HistoricalTournamentEntry> _historicalTournamentEntries
        = appDbContext.HistoricalTournamentEntries.AsNoTracking();

    public async Task<int> GetTournamentCountForSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken)
        => await _tournaments.CountAsync(tournament => tournament.SeasonId == seasonId, cancellationToken);

    public async Task<IReadOnlyDictionary<TournamentId, int>> GetTournamentEntryCountsAsync(IEnumerable<TournamentId> tournamentIds, CancellationToken cancellationToken)
    {
        var tournamentIdsByDatabaseId = await _tournaments
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .Select(tournament => new
            {
                DatabaseId = EF.Property<int>(tournament, ShadowIdConfiguration.DefaultPropertyName),
                TournamentId = tournament.Id
            })
            .ToDictionaryAsync(t => t.DatabaseId, t => t.TournamentId, cancellationToken);

        var entryCounts = await _historicalTournamentEntries
            .Where(tournament => tournamentIdsByDatabaseId.Keys.Contains(tournament.TournamentId))
            .ToDictionaryAsync(tournament => tournament.TournamentId, tournament => tournament.Entries, cancellationToken);

        var inverseMap = tournamentIdsByDatabaseId.ToDictionary(kv => kv.Value, kv => kv.Key);
        // we will need to look into the stats tables for 2026+ tournaments once they come over
        return tournamentIdsByDatabaseId.Values.ToDictionary(
            id => id,
            id => entryCounts.GetValueOrDefault(inverseMap[id], 0));
    }
}