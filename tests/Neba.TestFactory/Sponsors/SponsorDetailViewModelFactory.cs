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
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessUnit = null,
        string? businessCity = null,
        string? businessState = null,
        string? businessPostalCode = null,
        string? businessCountry = null,
        string? contactEmail = null,
        IReadOnlyCollection<PhoneNumberResponse>? phoneNumbers = null)
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
        };

    public static IReadOnlyCollection<SponsorDetailViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SponsorDetailViewModel
        {
            Id = Ulid.Bogus(faker),
            Slug = faker.Lorem.Slug(),
            Name = faker.Company.CompanyName(),
            IsCurrentSponsor = true,
            TierName = faker.PickRandom(SponsorTier.List.ToArray()).Name,
            CategoryName = faker.PickRandom(SponsorCategory.List.ToArray()).Name,
            LogoUrl = new Uri(faker.Internet.Avatar()),
            WebsiteUrl = new Uri(faker.Internet.Url()),
            Tagline = faker.Company.CatchPhrase(),
            AboutText = faker.Company.Bs(),
            FacebookUrl = new Uri(faker.Internet.Url()),
            InstagramUrl = new Uri(faker.Internet.Url()),
            BusinessStreet = faker.Address.StreetAddress(),
            BusinessUnit = null,
            BusinessCity = faker.Address.City(),
            BusinessState = faker.Address.StateAbbr(),
            BusinessPostalCode = faker.Address.ZipCode(),
            BusinessCountry = faker.Address.CountryCode(),
            ContactEmail = faker.Internet.Email(),
            PhoneNumbers = PhoneNumberResponseFactory.Bogus(1, faker),
        })];
    }

    public static IReadOnlyCollection<SponsorDetailViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}