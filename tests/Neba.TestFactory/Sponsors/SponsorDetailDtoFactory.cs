using Neba.Api.Contacts;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.Sponsors;

public static class SponsorDetailDtoFactory
{
    public const string ValidName = "Joe's Sponsorship Company";
    public const string ValidSlug = "joes-sponsorship-company";

    public static SponsorDetailDto Create(
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
        AddressDto? businessAddress = null,
        string? businessEmail = null,
        IReadOnlyCollection<PhoneNumberDto>? phoneNumbers = null)
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
                WebsiteUrl = websiteUrl,
                TagPhrase = tagPhrase,
                Description = description,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                BusinessAddress = businessAddress,
                BusinessEmailAddress = businessEmail,
                PhoneNumbers = phoneNumbers ?? [],
            };

    public static IReadOnlyCollection<SponsorDetailDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var businessAddressPool = UniquePool.CreateNullable(AddressDtoFactory.Bogus(count * 10, faker), poolSeed);
        var businessEmailPool = UniquePool.CreateNullable(EmailAddressFactory.Bogus(count * 10, faker), poolSeed);
        var phoneNumberPool = UniquePool.Create(PhoneNumberDtoFactory.Bogus(count * 10, faker), poolSeed);

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
                WebsiteUrl = new Uri(faker.Internet.Url()),
                TagPhrase = faker.Company.CatchPhrase(),
                Description = faker.Company.Bs(),
                FacebookUrl = new Uri(faker.Internet.UrlWithPath("facebook")),
                InstagramUrl = new Uri(faker.Internet.UrlWithPath("instagram")),
                BusinessAddress = businessAddressPool.GetNextNullable(),
                BusinessEmailAddress = businessEmailPool.GetNextNullable()?.Value,
                PhoneNumbers = [.. new[] { phoneNumberPool.GetNext(), phoneNumberPool.GetNext() }.DistinctBy(p => p.PhoneNumberType)],
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