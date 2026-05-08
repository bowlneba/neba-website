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

        // we will need to look into the stats tables for 2026+ tournaments once they come over
        return tournamentIdsByDatabaseId.Values.ToDictionary(
            tournamentId => tournamentId,
            tournamentId =>
            {
                var databaseId = tournamentIdsByDatabaseId.First(t => t.Value == tournamentId).Key;
                return entryCounts.GetValueOrDefault(databaseId, 0);
            });
    }

    public async Task<IReadOnlyCollection<SeasonTournamentDto>> GetTournamentsInSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken)
    {
        var tournaments = await _tournaments
            .Where(tournament => tournament.SeasonId == seasonId)
            .Select(tournament => new
            {
                DbId = EF.Property<int>(tournament, ShadowIdConfiguration.DefaultPropertyName),
                Dto = new SeasonTournamentDto
                {
                    Id = tournament.Id,
                    Name = tournament.Name,
                    Season = new SeasonDto
                    {
                        Id = tournament.Season.Id,
                        Description = tournament.Season.Description,
                        StartDate = tournament.Season.StartDate,
                        EndDate = tournament.Season.EndDate
                    },
                    StartDate = tournament.StartDate,
                    EndDate = tournament.EndDate,
                    StatsEligible = tournament.StatsEligible,
                    TournamentType = tournament.TournamentType.Name,
                    BowlingCenter = tournament.BowlingCenter == null
                        ? null
                        : new BowlingCenterSummaryDto
                        {
                            CertificationNumber = tournament.BowlingCenter.CertificationNumber.Value,
                            Name = tournament.BowlingCenter.Name,
                            Status = tournament.BowlingCenter.Status.Name,
                            Address = new AddressDto
                            {
                                Street = tournament.BowlingCenter.Address.Street,
                                Unit = tournament.BowlingCenter.Address.Unit,
                                City = tournament.BowlingCenter.Address.City,
                                Region = tournament.BowlingCenter.Address.Region,
                                Country = tournament.BowlingCenter.Address.Country.Value,
                                PostalCode = tournament.BowlingCenter.Address.PostalCode,
                            }
                        },
                    Sponsors = tournament.Sponsors
                        .Select(tournamentSponsor => tournamentSponsor.Sponsor)
                        .Select(sponsor => new SponsorSummaryDto
                        {
                            Name = sponsor.Name,
                            Slug = sponsor.Slug,
                        }).ToList(),
                    AddedMoney = tournament.Sponsors.Sum(tournamentSponsor => tournamentSponsor.SponsorshipAmount),
                    PatternLengthCategory = tournament.PatternLengthCategory == null
                        ? null
                        : tournament.PatternLengthCategory.Name,
                    PatternRatioCategory = tournament.PatternRatioCategory == null
                        ? null
                        : tournament.PatternRatioCategory.Name,
                    EntryFee = tournament.EntryFee,
                    RegistrationUrl = tournament.ExternalRegistrationUrl,
                    LogoContainer = tournament.Logo != null ? tournament.Logo.Container : null,
                    LogoPath = tournament.Logo != null ? tournament.Logo.Path : null,
                    Reservations = 999, // need to replace once actual column exists
                },
                OilPatternsRaw = tournament.OilPatterns.Select(tournamentOilPattern => new
                {
                    OilPattern = new OilPatternDto
                    {
                        Id = tournamentOilPattern.OilPattern.Id,
                        Name = tournamentOilPattern.OilPattern.Name,
                        Length = tournamentOilPattern.OilPattern.Length,
                        Volume = tournamentOilPattern.OilPattern.Volume,
                        LeftRatio = tournamentOilPattern.OilPattern.LeftRatio,
                        RightRatio = tournamentOilPattern.OilPattern.RightRatio,
                        KegelId = tournamentOilPattern.OilPattern.KegelId,
                    },
                    tournamentOilPattern.TournamentRounds
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        // We will need to do a separate query to get the champions from tournaments that we have full stats (2026+)
        var dbIds = tournaments.ConvertAll(t => t.DbId);

        var historicalWinners = await _historicalTournamentChampions
            .Where(tc => dbIds.Contains(tc.TournamentId))
            .Select(tc => new { tc.TournamentId, tc.Bowler.Name })
            .ToListAsync(cancellationToken);

        Dictionary<int, IReadOnlyCollection<Name>> historicalWinnersByTournamentDbId =
            historicalWinners
                .GroupBy(w => w.TournamentId)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Name>)[.. g.Select(w => w.Name)]);

        return tournaments
            .ConvertAll(t => t.Dto with
            {
                OilPatterns = t.OilPatternsRaw
                    .ConvertAll(op => new TournamentOilPatternDto
                    {
                        OilPattern = op.OilPattern,
                        TournamentRounds = [.. op.TournamentRounds.Select(tr => tr.Name)]
                    }),
                Winners = historicalWinnersByTournamentDbId.GetValueOrDefault(t.DbId, [])
            });
    }

    public async Task<TournamentDetailDto?> GetTournamentDetailAsync(TournamentId id, CancellationToken cancellationToken)
    {
        var tournament = await _tournaments
            .Where(t => t.Id == id)
            .Select(t => new
            {
                DbId = EF.Property<int>(t, ShadowIdConfiguration.DefaultPropertyName),
                Dto = new TournamentDetailDto
                {
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
                        .Select(s => new SponsorSummaryDto { Name = s.Name, Slug = s.Slug })
                        .ToList(),
                    AddedMoney = t.Sponsors.Sum(ts => ts.SponsorshipAmount),
                    PatternLengthCategory = t.PatternLengthCategory == null ? null : t.PatternLengthCategory.Name,
                    PatternRatioCategory = t.PatternRatioCategory == null ? null : t.PatternRatioCategory.Name,
                    EntryFee = t.EntryFee,
                    RegistrationUrl = t.ExternalRegistrationUrl,
                    LogoContainer = t.Logo != null ? t.Logo.Container : null,
                    LogoPath = t.Logo != null ? t.Logo.Path : null,
                    Reservations = 999, // need to replace once actual column exists
                },
                OilPatternsRaw = t.OilPatterns.Select(top => new
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
                    top.TournamentRounds
                }).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (tournament is null)
            return null;

        var historicalWinners = await _historicalTournamentChampions
            .Where(tc => tc.TournamentId == tournament.DbId)
            .Select(tc => tc.Bowler.Name)
            .ToListAsync(cancellationToken);

        var historicalResults = await _historicalTournamentResults
            .Where(r => r.TournamentId == tournament.DbId)
            .Select(r => new TournamentResultDto
            {
                BowlerName = r.Bowler.Name,
                Place = r.Place,
                PrizeMoney = r.PrizeMoney,
                Points = r.Points,
                SideCutName = r.SideCut != null ? r.SideCut.Name : null,
                SideCutIndicator = r.SideCut != null ? r.SideCut.Indicator : null,
            })
            .ToListAsync(cancellationToken);

        var historicalEntryCount = await _historicalTournamentEntries
            .Where(e => e.TournamentId == tournament.DbId)
            .Select(e => (int?)e.Entries)
            .SingleOrDefaultAsync(cancellationToken);

        return tournament.Dto with
        {
            OilPatterns = [.. tournament.OilPatternsRaw
                .Select(op => new TournamentOilPatternDto
                {
                    OilPattern = op.OilPattern,
                    TournamentRounds = [.. op.TournamentRounds.Select(tr => tr.Name)]
                })],
            Winners = [.. historicalWinners],
            // If Results or EntryCount are empty/null, check future stats tables for 2026+ tournament data
            Results = [.. historicalResults],
            EntryCount = historicalEntryCount,
        };
    }
}