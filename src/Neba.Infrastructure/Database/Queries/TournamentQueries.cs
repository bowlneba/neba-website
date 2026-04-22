using Microsoft.EntityFrameworkCore;

using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Contact;
using Neba.Application.Seasons;
using Neba.Application.Sponsors;
using Neba.Application.Tournaments;
using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class TournamentQueries(AppDbContext appDbContext)
    : ITournamentQueries
{
    private readonly IQueryable<Tournament> _tournaments
        = appDbContext.Tournaments.AsNoTracking();

    public async Task<int> GetTournamentCountForSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken)
        => await _tournaments.CountAsync(tournament => tournament.SeasonId == seasonId, cancellationToken);

    public async Task<IReadOnlyCollection<TournamentSummaryDto>> GetTournamentsInSeasonAsync(SeasonId seasonId, CancellationToken cancellationToken)
        => await _tournaments
            .Where(tournament => tournament.SeasonId == seasonId)
            .Select(tournament => new TournamentSummaryDto
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
                Winners = new List<Name>() // need to replace once actual relationship exists
            })
            .ToListAsync(cancellationToken);
}