using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Contacts;
using Neba.Domain.BowlingCenters;

namespace Neba.Api.Features.BowlingCenters.ListBowlingCenters;

internal sealed class ListBowlingCentersQueryHandler(AppDbContext appDbContext)
        : IQueryHandler<ListBowlingCentersQuery, IReadOnlyCollection<BowlingCenterSummaryDto>>
{
    private readonly IQueryable<BowlingCenter> _bowlingCenters = appDbContext.BowlingCenters.AsNoTracking();

    public async Task<IReadOnlyCollection<BowlingCenterSummaryDto>> HandleAsync(ListBowlingCentersQuery query, CancellationToken cancellationToken)
        => await _bowlingCenters
            .Select(bowlingCenter => new BowlingCenterSummaryDto
            {
                CertificationNumber = bowlingCenter.CertificationNumber.Value,
                Name = bowlingCenter.Name,
                Status = bowlingCenter.Status.Name,
                Address = new AddressDto
                {
                    Street = bowlingCenter.Address.Street,
                    Unit = bowlingCenter.Address.Unit,
                    City = bowlingCenter.Address.City,
                    Region = bowlingCenter.Address.Region,
                    Country = bowlingCenter.Address.Country.Value,
                    PostalCode = bowlingCenter.Address.PostalCode,
                    Latitude = bowlingCenter.Address.Coordinates!.Latitude,
                    Longitude = bowlingCenter.Address.Coordinates.Longitude
                },
                PhoneNumbers = bowlingCenter.PhoneNumbers.Select(phoneNumber => new PhoneNumberDto
                {
                    PhoneNumberType = phoneNumber.Type.Name,
                    Number = phoneNumber.ToCanonical()
                }).ToList(),
                Website = bowlingCenter.Website
            })
            .ToListAsync(cancellationToken);
}