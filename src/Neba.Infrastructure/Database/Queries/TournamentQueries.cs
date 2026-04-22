using Microsoft.EntityFrameworkCore;

using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Contact;
using Neba.Application.Seasons;
using Neba.Application.Sponsors;
using Neba.Application.Tournaments;
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

    public async Task<int> GetTournamentCountForSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken)
        => await _tournaments.CountAsync(tournament => tournament.SeasonId == seasonId, cancellationToken);

    public async Task<IReadOnlyCollection<TournamentSummaryDto>> GetTournamentsInSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken)
    {
        var tournaments = await _tournaments
            .Where(tournament => tournament.SeasonId == seasonId)
            .Select(tournament => new
            {
                DbId = EF.Property<int>(tournament, ShadowIdConfiguration.DefaultPropertyName),
                Dto = new TournamentSummaryDto
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

                    EntryFee = 9999.99m, // need to replace once actual column exists
                    RegistrationUrl = null, // need to replace once actual column exists
                    Reservations = 999, // need to replace once actual column exists
                    OilPattern = null, // need to replace once actual relationship exists
                    LogoUrl = null, // need to replace once actual relationship exists
                    LogoContainer = null, // need to replace once actual relationship exists
                    LogoPath = null, // need to replace once actual relationship exists
                }
            })
            .ToListAsync(cancellationToken);

        // We will need to do a separate query to get the champions from tournaments that we have full stats (2026+)
        var dbIds = tournaments.ConvertAll(t => t.DbId);

        var winners = await _historicalTournamentChampions
            .Where(tc => dbIds.Contains(tc.TournamentId))
            .Select(tc => new { tc.TournamentId, tc.Bowler.Name })
            .ToListAsync(cancellationToken);

        Dictionary<int, IReadOnlyCollection<Name>> winnersByTournamentDbId = winners
            .GroupBy(w => w.TournamentId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Name>)[.. g.Select(w => w.Name)]);

        return tournaments
            .ConvertAll(t => t.Dto with { Winners = winnersByTournamentDbId.GetValueOrDefault(t.DbId, []) });
    }
}