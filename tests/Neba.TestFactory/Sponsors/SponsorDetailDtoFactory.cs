using Neba.Api.Contacts;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.Sponsors;

public static class SponsorDetailDtoFactory
{
    public const string ValidName = "Joe's Sponsorship Company";
    public const string ValidSlug = "joes-sponsorship-company";

#pragma warning disable S107
    public static SponsorDetailDto Create(
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
        AddressDto? businessAddress = null,
        string? businessEmail = null,
        IReadOnlyCollection<PhoneNumberDto>? phoneNumbers = null,
        SponsorContactDto? contact = null,
        IReadOnlyCollection<SponsorDetailTournamentDto>? tournamentsSponsored = null)
            => new()
            {
                Id = id ?? SponsorId.New(),
                Name = name ?? ValidName,
                Slug = slug ?? ValidSlug,
                IsCurrentSponsor = isCurrentSponsor ?? SponsorFactory.ValidIsCurrentSponsor,
                Priority = priority ?? SponsorFactory.ValidPriority,
                Tier = tier?.Name ?? SponsorFactory.ValidTier.Name,
                Category = category?.Name ?? SponsorFactory.ValidCategory.Name,
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
                BusinessAddress = businessAddress,
                BusinessEmailAddress = businessEmail,
                PhoneNumbers = phoneNumbers ?? [],
                Contact = contact,
                TournamentsSponsored = tournamentsSponsored ?? [],
            };
#pragma warning restore S107

    internal static IReadOnlyCollection<SponsorDetailDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var businessAddressPool = UniquePool.CreateNullable(AddressDtoFactory.Bogus(count * 10, faker), poolSeed);
        var businessEmailPool = UniquePool.CreateNullable(EmailAddressFactory.Bogus(count * 10, faker), poolSeed);
        var phoneNumberPool = UniquePool.Create(PhoneNumberDtoFactory.Bogus(count * 10, faker), poolSeed);
        var contactPool = UniquePool.CreateNullable(SponsorContactDtoFactory.Bogus(count * 10, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var hasLogo = faker.Random.Bool();
            return new SponsorDetailDto
            {
                Id = new SponsorId(Ulid.BogusString(faker)),
                Name = faker.Company.CompanyName(),
                Slug = faker.Lorem.Slug(),
                IsCurrentSponsor = faker.Random.Bool(),
                Priority = faker.Random.Int(1, 10),
                Tier = faker.PickRandom(SponsorTier.List.ToArray()).Name,
                Category = faker.PickRandom(SponsorCategory.List.ToArray()).Name,
                LogoUrl = hasLogo ? new Uri(faker.Internet.Url()) : null,
                LogoContainer = hasLogo ? "sponsor-logos" : null,
                LogoPath = hasLogo ? $"sponsors/{faker.Random.Guid()}/logo/{faker.System.FileName("png")}" : null,
                LogoContentType = hasLogo ? "image/png" : null,
                LogoSizeInBytes = hasLogo ? faker.Random.Long(1024, 5_242_880) : null,
                WebsiteUrl = new Uri(faker.Internet.Url()),
                TagPhrase = faker.Company.CatchPhrase(),
                Description = faker.Company.Bs(),
                LiveReadText = faker.Lorem.Sentences(2),
                PromotionalNotes = faker.Lorem.Sentences(3),
                FacebookUrl = new Uri(faker.Internet.UrlWithPath("facebook")),
                InstagramUrl = new Uri(faker.Internet.UrlWithPath("instagram")),
                BusinessAddress = businessAddressPool.GetNextNullable(),
                BusinessEmailAddress = businessEmailPool.GetNextNullable()?.Value,
                PhoneNumbers = [.. new[] { phoneNumberPool.GetNext(), phoneNumberPool.GetNext() }.DistinctBy(p => p.PhoneNumberType)],
                Contact = contactPool.GetNextNullable(),
                TournamentsSponsored = SponsorDetailTournamentDtoFactory.Bogus(faker.Random.Int(0, 3), faker),
            };
        })];
    }

    public static IReadOnlyCollection<SponsorDetailDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}