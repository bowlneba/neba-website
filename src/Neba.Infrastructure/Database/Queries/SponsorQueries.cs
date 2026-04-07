using Microsoft.EntityFrameworkCore;

using Neba.Application.Contact;
using Neba.Application.Sponsors;
using Neba.Application.Sponsors.GetSponsorDetail;
using Neba.Domain.Sponsors;

namespace Neba.Infrastructure.Database.Queries;

internal sealed class SponsorQueries(AppDbContext appDbContext)
    : ISponsorQueries
{
    private readonly IQueryable<Sponsor> _sponsors = appDbContext.Sponsors.AsNoTracking();

    public async Task<IReadOnlyCollection<SponsorSummaryDto>> GetActiveSponsorsAsync(CancellationToken cancellationToken)
        => await _sponsors
            .Where(sponsor => sponsor.IsCurrentSponsor)
            .Select(sponsor => new SponsorSummaryDto
            {
                Name = sponsor.Name,
                Slug = sponsor.Slug,
                LogoContainer = sponsor.Logo != null ? sponsor.Logo.Container : null,
                LogoPath = sponsor.Logo != null ? sponsor.Logo.Path : null,
                IsCurrentSponsor = sponsor.IsCurrentSponsor,
                Priority = sponsor.Priority,
                Tier = sponsor.Tier.Name,
                Category = sponsor.Category.Name,
                TagPhrase = sponsor.TagPhrase,
                Description = sponsor.Description,
                WebsiteUrl = sponsor.WebsiteUrl,
                FacebookUrl = sponsor.FacebookUrl,
                InstagramUrl = sponsor.InstagramUrl
            })
            .ToListAsync(cancellationToken);

    public async Task<SponsorDetailDto?> GetSponsorAsync(string slug, CancellationToken cancellationToken)
        => await _sponsors
            .Where(sponsor => sponsor.Slug == slug)
            .MapToDetailDtoAsync(cancellationToken);
}

internal static class SponsorQueriesExtensions
{
    extension(IQueryable<Sponsor> sponsors)
    {
        public async Task<SponsorDetailDto?> MapToDetailDtoAsync(CancellationToken cancellationToken)
            => await sponsors.Select(sponsor => new SponsorDetailDto
            {
                Id = sponsor.Id,
                Name = sponsor.Name,
                Slug = sponsor.Slug,
                Logo = sponsor.Logo,
                IsCurrentSponsor = sponsor.IsCurrentSponsor,
                Priority = sponsor.Priority,
                Tier = sponsor.Tier.Name,
                Category = sponsor.Category.Name,
                TagPhrase = sponsor.TagPhrase,
                Description = sponsor.Description,
                LiveReadText = sponsor.LiveReadText,
                PromotionalNotes = sponsor.PromotionalNotes,
                WebsiteUrl = sponsor.WebsiteUrl,
                FacebookUrl = sponsor.FacebookUrl,
                InstagramUrl = sponsor.InstagramUrl,
                BusinessAddress = sponsor.BusinessAddress != null
                    ? new AddressDto
                    {
                        Street = sponsor.BusinessAddress.Street,
                        Unit = sponsor.BusinessAddress.Unit,
                        City = sponsor.BusinessAddress.City,
                        Region = sponsor.BusinessAddress.Region,
                        PostalCode = sponsor.BusinessAddress.PostalCode,
                        Country = sponsor.BusinessAddress.Country
                    }
                    : null,
                BusinessEmailAddress = sponsor.BusinessEmail != null ? sponsor.BusinessEmail.Value : null,
                PhoneNumbers = sponsor.PhoneNumbers.Select(pn => new PhoneNumberDto
                {
                    Number = pn.Number,
                    PhoneNumberType = pn.Type.Name
                }).ToList(),
                SponsorContactInfo = sponsor.SponsorContact != null
                    ? new ContactInfoDto
                    {
                        Name = sponsor.SponsorContact.Name,
                        EmailAddress = sponsor.SponsorContact.Email.Value,
                        PhoneNumber = new PhoneNumberDto
                        {
                            Number = sponsor.SponsorContact.Phone.Number,
                            PhoneNumberType = sponsor.SponsorContact.Phone.Type.Name
                        }
                    }
                    : null
            }
            ).SingleOrDefaultAsync(cancellationToken);
    }
}