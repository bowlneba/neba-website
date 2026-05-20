using System.Globalization;

using Neba.Api.Contracts.Contact;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Contact;
using Neba.Website.Server.Sponsors;

namespace Neba.TestFactory.Sponsors;

public static class SponsorDetailViewModelFactory
{
    public const string ValidId = "01KNPMEYKAR8YHHZ0FSPX91MNN";
    public const string ValidName = "Joe's Sponsor, LLC";
    public const string ValidSlug = "joes-sponsor";
    public const string ValidAboutText = "Joe's Sponsor is a leading provider of bowling sponsorships.";
    public const string ValidBusinessStreet = "123 Main St";
    public const string ValidBusinessCity = "Anytown";
    public const string ValidBusinessState = "CA";
    public const string ValidBusinessPostalCode = "12345";
    public const string ValidContactEmail = "info@joessponsor.com";

    public static SponsorDetailViewModel Create(
        Ulid? id = null,
        string? slug = null,
        string? name = null,
        bool? isCurrentSponsor = null,
        SponsorTier? tier = null,
        SponsorCategory? category = null,
        Uri? logoUrl = null,
        Uri? websiteUrl = null,
        string? tagline = null,
        string? aboutText = null,
        string? promotionalNotes = null,
        string? liveReadScript = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessUnit = null,
        string? businessCity = null,
        string? businessState = null,
        string? businessPostalCode = null,
        string? businessCountry = null,
        string? contactEmail = null,
        IReadOnlyCollection<PhoneNumberResponse>? phoneNumbers = null,
        string? sponsorContactName = null,
        string? sponsorContactEmail = null,
        string? sponsorContactPhone = null,
        string? sponsorContactPhoneType = null)
        => new()
        {
            Id = id ?? Ulid.Parse(ValidId, CultureInfo.InvariantCulture),
            Slug = slug ?? ValidSlug,
            Name = name ?? ValidName,
            IsCurrentSponsor = isCurrentSponsor ?? SponsorFactory.ValidIsCurrentSponsor,
            TierName = tier?.Name ?? SponsorTier.Standard.Name,
            CategoryName = category?.Name ?? SponsorCategory.Technology.Name,
            LogoUrl = logoUrl,
            WebsiteUrl = websiteUrl,
            Tagline = tagline,
            AboutText = aboutText ?? ValidAboutText,
            PromotionalNotes = promotionalNotes,
            LiveReadScript = liveReadScript,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl,
            BusinessStreet = businessStreet ?? ValidBusinessStreet,
            BusinessUnit = businessUnit,
            BusinessCity = businessCity ?? ValidBusinessCity,
            BusinessState = businessState ?? ValidBusinessState,
            BusinessPostalCode = businessPostalCode ?? ValidBusinessPostalCode,
            BusinessCountry = businessCountry,
            ContactEmail = contactEmail ?? ValidContactEmail,
            PhoneNumbers = phoneNumbers ?? [PhoneNumberResponseFactory.Create()],
            SponsorContactName = sponsorContactName,
            SponsorContactEmail = sponsorContactEmail,
            SponsorContactPhone = sponsorContactPhone,
            SponsorContactPhoneType = sponsorContactPhoneType
        };

    public static IReadOnlyCollection<SponsorDetailViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SponsorDetailViewModel>()
            .CustomInstantiator(f => new()
            {
                Id = Ulid.Bogus(f),
                Slug = f.Lorem.Slug(),
                Name = f.Company.CompanyName(),
                IsCurrentSponsor = true,
                TierName = f.PickRandom(SponsorTier.List.ToArray()).Name,
                CategoryName = f.PickRandom(SponsorCategory.List.ToArray()).Name,
                LogoUrl = new Uri(f.Internet.Avatar()),
                WebsiteUrl = new Uri(f.Internet.Url()),
                Tagline = f.Company.CatchPhrase(),
                AboutText = f.Company.Bs(),
                PromotionalNotes = null,
                LiveReadScript = null,
                FacebookUrl = new Uri(f.Internet.Url()),
                InstagramUrl = new Uri(f.Internet.Url()),
                BusinessStreet = f.Address.StreetAddress(),
                BusinessUnit = null,
                BusinessCity = f.Address.City(),
                BusinessState = f.Address.StateAbbr(),
                BusinessPostalCode = f.Address.ZipCode(),
                BusinessCountry = f.Address.CountryCode(),
                ContactEmail = f.Internet.Email(),
                PhoneNumbers = PhoneNumberResponseFactory.Bogus(1, seed),
                SponsorContactName = null,
                SponsorContactEmail = null,
                SponsorContactPhone = null,
                SponsorContactPhoneType = null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}