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

    private readonly IQueryable<HistoricalTournamentResult> _historicalTournamentResults
        = appDbContext.HistoricalTournamentResults.AsNoTracking();

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

    public async Task<TournamentDetailDto?> GetTournamentDetailAsync(TournamentId id, CancellationToken cancellationToken)
    {
        var row = await ProjectTournaments(_tournaments.Where(t => t.Id == id))
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var historicalWinners = await _historicalTournamentChampions
            .Where(tc => tc.TournamentId == row.DbId)
            .Select(tc => tc.Bowler.Name)
            .ToListAsync(cancellationToken);

        var historicalResults = await _historicalTournamentResults
            .Where(r => r.TournamentId == row.DbId)
            .Select(r => new TournamentResultDto
            {
                BowlerName = r.Bowler.Name,
                Place = r.Place,
                PrizeMoney = r.PrizeMoney,
                Points = r.Points,
                SideCutName = r.SideCut != null ? r.SideCut.Name : null,
                SideCutIndicator = r.SideCut != null ? r.SideCut.Indicator : null,
            })
            .OrderBy(result => result.Place == null)
                .ThenBy(result => result.Place)
                .ThenBy(result => result.BowlerName.LastName)
                .ThenBy(result => result.BowlerName.FirstName)
            .ToListAsync(cancellationToken);

        var historicalEntryCount = await _historicalTournamentEntries
            .Where(e => e.TournamentId == row.DbId)
            .Select(e => (int?)e.Entries)
            .SingleOrDefaultAsync(cancellationToken);

        return ToTournamentDetailDto(row, [.. historicalWinners], [.. historicalResults], historicalEntryCount);
    }

    private static IQueryable<TournamentQueryRow> ProjectTournaments(IQueryable<Tournament> source)
        => source.Select(TournamentQueryProjection);

    private static readonly Expression<Func<Tournament, TournamentQueryRow>> TournamentQueryProjection = t => new TournamentQueryRow
    {
        DbId = EF.Property<int>(t, ShadowIdConfiguration.DefaultPropertyName),
        Id = t.Id,
        Name = t.Name,
        Season = new SeasonDto
        {
            Id = t.Season.Id,
            Description = t.Season.Description,
            StartDate = t.Season.StartDate,
            EndDate = t.Season.EndDate
        },
        StartDate = t.StartDate,
        EndDate = t.EndDate,
        StatsEligible = t.StatsEligible,
        TournamentType = t.TournamentType.Name,
        BowlingCenter = t.BowlingCenter == null
            ? null
            : new BowlingCenterSummaryDto
            {
                CertificationNumber = t.BowlingCenter.CertificationNumber.Value,
                Name = t.BowlingCenter.Name,
                Status = t.BowlingCenter.Status.Name,
                Address = new AddressDto
                {
                    Street = t.BowlingCenter.Address.Street,
                    Unit = t.BowlingCenter.Address.Unit,
                    City = t.BowlingCenter.Address.City,
                    Region = t.BowlingCenter.Address.Region,
                    Country = t.BowlingCenter.Address.Country.Value,
                    PostalCode = t.BowlingCenter.Address.PostalCode,
                }
            },
        Sponsors = t.Sponsors
            .Select(ts => ts.Sponsor)
            .Select(s => new SponsorSummaryDto
            {
                Name = s.Name,
                Slug = s.Slug,
                LogoContainer = s.Logo!.Container,
                LogoPath = s.Logo!.Path,
                WebsiteUrl = s.WebsiteUrl,
                TagPhrase = s.TagPhrase,
            })
            .ToList(),
        AddedMoney = t.Sponsors.Sum(ts => ts.SponsorshipAmount),
        PatternLengthCategory = t.PatternLengthCategory == null ? null : t.PatternLengthCategory.Name,
        PatternRatioCategory = t.PatternRatioCategory == null ? null : t.PatternRatioCategory.Name,
        EntryFee = t.EntryFee,
        RegistrationUrl = t.ExternalRegistrationUrl,
        LogoContainer = t.Logo!.Container,
        LogoPath = t.Logo!.Path,
        Reservations = 999, // need to replace once actual column exists
        OilPatternsRaw = t.OilPatterns.Select(top => new OilPatternRawRow
        {
            OilPattern = new OilPatternDto
            {
                Id = top.OilPattern.Id,
                Name = top.OilPattern.Name,
                Length = top.OilPattern.Length,
                Volume = top.OilPattern.Volume,
                LeftRatio = top.OilPattern.LeftRatio,
                RightRatio = top.OilPattern.RightRatio,
                KegelId = top.OilPattern.KegelId,
            },
            TournamentRounds = top.TournamentRounds
        }).ToList()
    };

    private static IReadOnlyCollection<TournamentOilPatternDto> MapOilPatterns(IReadOnlyCollection<OilPatternRawRow> raw)
        => [.. raw.Select(op => new TournamentOilPatternDto
        {
            OilPattern = op.OilPattern,
            TournamentRounds = [.. op.TournamentRounds.Select(tr => tr.Name)]
        })];

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

    private static TournamentDetailDto ToTournamentDetailDto(
        TournamentQueryRow row,
        IReadOnlyCollection<Name> winners,
        IReadOnlyCollection<TournamentResultDto> results,
        int? entryCount)
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
            // If Results or EntryCount are empty/null, check future stats tables for 2026+ tournament data
            Results = results,
            EntryCount = entryCount,
        };

    private sealed record TournamentQueryRow
    {
        public int DbId { get; init; }
        public TournamentId Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public SeasonDto Season { get; init; } = null!;
        public DateOnly StartDate { get; init; }
        public DateOnly EndDate { get; init; }
        public bool StatsEligible { get; init; }
        public string TournamentType { get; init; } = string.Empty;
        public BowlingCenterSummaryDto? BowlingCenter { get; init; }
        public IReadOnlyCollection<SponsorSummaryDto> Sponsors { get; init; } = [];
        public decimal? AddedMoney { get; init; }
        public int? Reservations { get; init; }
        public string? PatternLengthCategory { get; init; }
        public string? PatternRatioCategory { get; init; }
        public decimal? EntryFee { get; init; }
        public Uri? RegistrationUrl { get; init; }
        public string? LogoContainer { get; init; }
        public string? LogoPath { get; init; }
        public IReadOnlyCollection<OilPatternRawRow> OilPatternsRaw { get; init; } = [];
    }

    private sealed record OilPatternRawRow
    {
        public OilPatternDto OilPattern { get; init; } = null!;
        public IReadOnlyCollection<TournamentRound> TournamentRounds { get; init; } = [];
    }
}