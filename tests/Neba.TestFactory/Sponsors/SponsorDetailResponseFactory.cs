using System.Globalization;

using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.Sponsors;

public static class SponsorDetailResponseFactory
{
    public const string ValidId = "01KNPMEYKAR8YHHZ0FSPX91MNN";
    public const string ValidName = "Joe's Sponsor, LLC";
    public const string ValidSlug = "joes-sponsor";
    public const int ValidPriority = 5;
    public const string ValidBusinessStreet = "123 Main St";
    public const string ValidBusinessCity = "Anytown";
    public const string ValidBusinessState = "CA";
    public const string ValidBusinessPostalCode = "12345";
    public const string ValidBusinessCountry = "US";
    public const string ValidBusinessEmailAddress = "joe@sponsor.com";

    public static SponsorDetailResponse Create(
        SponsorId? id = null,
        string? name = null,
        string? slug = null,
        bool? isCurrentSponsor = null,
        int? priority = null,
        SponsorTier? tier = null,
        SponsorCategory? category = null,
        Uri? logoUrl = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessCity = null,
        string? businessState = null,
        string? businessPostalCode = null,
        string? businessCountry = null,
        string? businessEmailAddress = null,
        IReadOnlyCollection<PhoneNumberResponse>? phoneNumbers = null)
            => new()
            {
                Id = id?.Value ?? Ulid.Parse(ValidId, CultureInfo.InvariantCulture),
                Name = name ?? ValidName,
                Slug = slug ?? ValidSlug,
                IsCurrentSponsor = isCurrentSponsor ?? true,
                Priority = priority ?? ValidPriority,
                Tier = tier?.Name ?? SponsorTier.Standard.Name,
                Category = category?.Name ?? SponsorCategory.Technology.Name,
                LogoUrl = logoUrl,
                WebsiteUrl = websiteUrl,
                TagPhrase = tagPhrase,
                Description = description,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                BusinessStreet = businessStreet ?? ValidBusinessStreet,
                BusinessCity = businessCity ?? ValidBusinessCity,
                BusinessState = businessState ?? ValidBusinessState,
                BusinessPostalCode = businessPostalCode ?? ValidBusinessPostalCode,
                BusinessCountry = businessCountry ?? ValidBusinessCountry,
                BusinessEmailAddress = businessEmailAddress ?? ValidBusinessEmailAddress,
                PhoneNumbers = phoneNumbers ?? [PhoneNumberResponseFactory.Create()],
            };

    public static IReadOnlyCollection<SponsorDetailResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SponsorDetailResponse
        {
            Id = Ulid.Bogus(faker),
            Name = faker.Company.CompanyName(),
            Slug = faker.Lorem.Slug(),
            IsCurrentSponsor = faker.Random.Bool(),
            Priority = faker.Random.Int(1, 10),
            Tier = faker.PickRandom(SponsorTier.List.ToArray()).Name,
            Category = faker.PickRandom(SponsorCategory.List.ToArray()).Name,
            LogoUrl = new Uri(faker.Internet.Avatar()),
            WebsiteUrl = new Uri(faker.Internet.Url()),
            TagPhrase = faker.Company.CatchPhrase(),
            Description = faker.Company.Bs(),
            FacebookUrl = new Uri(faker.Internet.Url()),
            InstagramUrl = new Uri(faker.Internet.Url()),
            BusinessStreet = faker.Address.StreetAddress(),
            BusinessCity = faker.Address.City(),
            BusinessState = faker.Address.StateAbbr(),
            BusinessPostalCode = faker.Address.ZipCode(),
            BusinessCountry = faker.Address.CountryCode(),
            BusinessEmailAddress = faker.Internet.Email(),
            PhoneNumbers = PhoneNumberResponseFactory.Bogus(2, faker),
        })];
    }

    public static IReadOnlyCollection<SponsorDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}