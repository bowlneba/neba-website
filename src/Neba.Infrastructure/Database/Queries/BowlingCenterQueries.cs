using Microsoft.EntityFrameworkCore;

using Neba.Application.BowlingCenters;
using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Contact;
using Neba.Domain.BowlingCenters;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class BowlingCenterQueries(AppDbContext dbContext)
        : IBowlingCenterQueries
{
    private readonly IQueryable<BowlingCenter> _bowlingCenters 
        = dbContext.BowlingCenters.AsNoTracking();

    public async Task<IReadOnlyCollection<BowlingCenterSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
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