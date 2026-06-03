using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Contacts;
using Neba.Api.Database;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Sponsors.GetSponsorDetail;

internal sealed class GetSponsorDetailQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
    : IQueryHandler<GetSponsorDetailQuery, ErrorOr<SponsorDetailDto>>
{
    private readonly IQueryable<Sponsor> _sponsors = appDbContext.Sponsors.AsNoTracking();
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<ErrorOr<SponsorDetailDto>> HandleAsync(GetSponsorDetailQuery query, CancellationToken cancellationToken)
    {
        var row = await _sponsors
            .Where(sponsor => sponsor.Slug == query.Slug)
            .Select(sponsor => new
            {
                sponsor.Id,
                sponsor.Name,
                sponsor.Slug,
                LogoContainer = sponsor.Logo != null ? sponsor.Logo.Container : null,
                LogoPath = sponsor.Logo != null ? sponsor.Logo.Path : null,
                sponsor.IsCurrentSponsor,
                sponsor.Priority,
                Tier = sponsor.Tier.Name,
                Category = sponsor.Category.Name,
                sponsor.TagPhrase,
                sponsor.Description,
                sponsor.LiveReadText,
                sponsor.PromotionalNotes,
                sponsor.WebsiteUrl,
                sponsor.FacebookUrl,
                sponsor.InstagramUrl,
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

        if (row is null)
        {
            return SponsorErrors.SponsorNotFound(query.Slug);
        }

        return new SponsorDetailDto
        {
            Id = row.Id,
            Name = row.Name,
            Slug = row.Slug,
            LogoUrl = row.LogoContainer is not null && row.LogoPath is not null
                ? _fileStorageService.GetBlobUri(row.LogoContainer, row.LogoPath)
                : null,
            IsCurrentSponsor = row.IsCurrentSponsor,
            Priority = row.Priority,
            Tier = row.Tier,
            Category = row.Category,
            TagPhrase = row.TagPhrase,
            Description = row.Description,
            LiveReadText = row.LiveReadText,
            PromotionalNotes = row.PromotionalNotes,
            WebsiteUrl = row.WebsiteUrl,
            FacebookUrl = row.FacebookUrl,
            InstagramUrl = row.InstagramUrl,
            BusinessAddress = row.BusinessAddress,
            BusinessEmailAddress = row.BusinessEmailAddress,
            PhoneNumbers = row.PhoneNumbers,
            SponsorContactInfo = row.SponsorContactInfo
        };
    }
}