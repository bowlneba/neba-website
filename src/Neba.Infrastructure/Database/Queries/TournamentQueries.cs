using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Contact;
using Neba.Application.Seasons;
using Neba.Application.Sponsors;
using Neba.Application.Tournaments;
using Neba.Application.Tournaments.GetTournament;
using Neba.Application.Tournaments.ListTournamentsInSeason;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database.Configurations;
using Neba.Infrastructure.Database.Entities;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class TournamentQueries(AppDbContext appDbContext)
    : ITournamentQueries
{
    private readonly IQueryable<Tournament> _tournaments
        = appDbContext.Tournaments.AsNoTracking();

    private readonly IQueryable<HistoricalTournamentChampion> _historicalTournamentChampions
        = appDbContext.HistoricalTournamentChampions.AsNoTracking();

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

    public async Task<IReadOnlyCollection<SeasonTournamentDto>> GetTournamentsInSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken)
    {
        var rows = await ProjectTournaments(_tournaments.Where(t => t.SeasonId == seasonId))
            .ToListAsync(cancellationToken);

        // We will need to do a separate query to get the champions from tournaments that we have full stats (2026+)
        var dbIds = rows.ConvertAll(r => r.DbId);

        var historicalWinners = await _historicalTournamentChampions
            .Where(tc => dbIds.Contains(tc.TournamentId))
            .Select(tc => new { tc.TournamentId, tc.Bowler.Name })
            .ToListAsync(cancellationToken);

        Dictionary<int, IReadOnlyCollection<Name>> historicalWinnersByTournamentDbId =
            historicalWinners
                .GroupBy(w => w.TournamentId)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Name>)[.. g.Select(w => w.Name)]);

        return rows.ConvertAll(r =>
            ToSeasonTournamentDto(r, historicalWinnersByTournamentDbId.GetValueOrDefault(r.DbId, [])));
    }

    private static SeasonTournamentDto ToSeasonTournamentDto(TournamentQueryRow row, IReadOnlyCollection<Name> winners)
        => new()
        {
            Id = row.Id,
            Name = row.Name,
            Season = row.Season,
            StartDate = row.StartDate,
            EndDate = row.EndDate,
            StatsEligible = row.StatsEligible,
            TournamentType = row.TournamentType,
            EntryFee = row.EntryFee,
            RegistrationUrl = row.RegistrationUrl,
            BowlingCenter = row.BowlingCenter,
            Sponsors = row.Sponsors,
            AddedMoney = row.AddedMoney,
            Reservations = row.Reservations,
            PatternLengthCategory = row.PatternLengthCategory,
            PatternRatioCategory = row.PatternRatioCategory,
            OilPatterns = MapOilPatterns(row.OilPatternsRaw),
            LogoContainer = row.LogoContainer,
            LogoPath = row.LogoPath,
            Winners = winners,
        };
}