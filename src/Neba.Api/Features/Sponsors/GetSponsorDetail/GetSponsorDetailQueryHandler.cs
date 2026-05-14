using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contacts;
using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Storage;
using Neba.Domain.Sponsors;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
    : IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>
{
    private readonly IQueryable<Sponsor> _sponsors = appDbContext.Sponsors.AsNoTracking();
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<ErrorOr<SponsorDetailDto>> HandleAsync(GetSponsorDetailQuery query, CancellationToken cancellationToken)
    {
        var sponsor = await _sponsors
            .Where(sponsor => sponsor.Slug == query.Slug)
            .Select(sponsor => new SponsorDetailDto
            {
                Id = sponsor.Id,
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
                PhoneNumbers = sponsor.PhoneNumbers.Select(phoneNumber => new PhoneNumberDto
                {
                    Number = phoneNumber.Number,
                    PhoneNumberType = phoneNumber.Type.Name
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
            }).SingleOrDefaultAsync(cancellationToken);

        if (sponsor is null)
        {
            return SponsorErrors.SponsorNotFound(query.Slug);
        }

        return sponsor.LogoContainer is not null && sponsor.LogoPath is not null
            ? sponsor with { LogoUrl = _fileStorageService.GetBlobUri(sponsor.LogoContainer, sponsor.LogoPath) }
            : sponsor;
    }
}