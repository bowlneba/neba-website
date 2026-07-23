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

#pragma warning disable S107
    public static SponsorDetailResponse Create(
        SponsorId? id = null,
        string? name = null,
        string? slug = null,
        bool? isCurrentSponsor = null,
        int? priority = null,
        SponsorTier? tier = null,
        SponsorCategory? category = null,
        Uri? logoUrl = null,
        string? logoContainer = null,
        string? logoPath = null,
        string? logoContentType = null,
        long? logoSizeInBytes = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        string? liveReadText = null,
        string? promotionalNotes = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessCity = null,
        string? businessState = null,
        string? businessPostalCode = null,
        string? businessCountry = null,
        string? businessEmailAddress = null,
        IReadOnlyCollection<PhoneNumberResponse>? phoneNumbers = null,
        SponsorContactResponse? contact = null,
        IReadOnlyCollection<SponsorDetailTournamentResponse>? tournamentsSponsored = null)
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
                LogoContainer = logoContainer,
                LogoPath = logoPath,
                LogoContentType = logoContentType,
                LogoSizeInBytes = logoSizeInBytes,
                WebsiteUrl = websiteUrl,
                TagPhrase = tagPhrase,
                Description = description,
                LiveReadText = liveReadText,
                PromotionalNotes = promotionalNotes,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                BusinessStreet = businessStreet ?? ValidBusinessStreet,
                BusinessCity = businessCity ?? ValidBusinessCity,
                BusinessState = businessState ?? ValidBusinessState,
                BusinessPostalCode = businessPostalCode ?? ValidBusinessPostalCode,
                BusinessCountry = businessCountry ?? ValidBusinessCountry,
                BusinessEmailAddress = businessEmailAddress ?? ValidBusinessEmailAddress,
                PhoneNumbers = phoneNumbers ?? [PhoneNumberResponseFactory.Create()],
                Contact = contact,
                TournamentsSponsored = tournamentsSponsored ?? [],
            };
#pragma warning restore S107

    internal static IReadOnlyCollection<SponsorDetailResponse> Bogus(int count, Faker faker)
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
            LogoContainer = "sponsor-logos",
            LogoPath = $"sponsors/{faker.Random.Guid()}/logo/{faker.System.FileName("png")}",
            LogoContentType = "image/png",
            LogoSizeInBytes = faker.Random.Long(1024, 5_242_880),
            WebsiteUrl = new Uri(faker.Internet.Url()),
            TagPhrase = faker.Company.CatchPhrase(),
            Description = faker.Company.Bs(),
            LiveReadText = faker.Lorem.Sentences(2),
            PromotionalNotes = faker.Lorem.Sentences(3),
            FacebookUrl = new Uri(faker.Internet.Url()),
            InstagramUrl = new Uri(faker.Internet.Url()),
            BusinessStreet = faker.Address.StreetAddress(),
            BusinessCity = faker.Address.City(),
            BusinessState = faker.Address.StateAbbr(),
            BusinessPostalCode = faker.Address.ZipCode(),
            BusinessCountry = faker.Address.CountryCode(),
            BusinessEmailAddress = faker.Internet.Email(),
            PhoneNumbers = PhoneNumberResponseFactory.Bogus(2, faker),
            Contact = SponsorContactResponseFactory.Bogus(1, faker).Single(),
            TournamentsSponsored = SponsorDetailTournamentResponseFactory.Bogus(faker.Random.Int(0, 3), faker),
        })];
    }

    public static IReadOnlyCollection<SponsorDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}