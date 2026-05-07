using Microsoft.EntityFrameworkCore;

using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Contact;
using Neba.Application.Seasons;
using Neba.Application.Sponsors;
using Neba.Application.Tournaments;
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
}