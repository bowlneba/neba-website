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

    public static IReadOnlyCollection<SponsorDetailDto> Bogus(int count, int? seed = null)
    {
        var businessAddressPool = UniquePool.CreateNullable(AddressDtoFactory.Bogus(count * 10, seed), seed);
        var businessEmailPool = UniquePool.CreateNullable(EmailAddressFactory.Bogus(count * 10, seed), seed);
        var phoneNumberPool = UniquePool.Create(PhoneNumberDtoFactory.Bogus(count * 10, seed), seed);

        var faker = new Faker<SponsorDetailDto>()
            .CustomInstantiator(f =>
            {
                var hasLogo = f.Random.Bool();
                return new()
                {
                    Id = new SponsorId(Ulid.BogusString(f)),
                    Name = f.Company.CompanyName(),
                    Slug = f.Lorem.Slug(),
                    IsCurrentSponsor = f.Random.Bool(),
                    Priority = f.Random.Int(1, 10),
                    Tier = f.PickRandom(SponsorTier.List.ToArray()).Name,
                    Category = f.PickRandom(SponsorCategory.List.ToArray()).Name,
                    LogoUrl = hasLogo ? new Uri(f.Internet.Url()) : null,
                    WebsiteUrl = new Uri(f.Internet.Url()),
                    TagPhrase = f.Company.CatchPhrase(),
                    Description = f.Company.Bs(),
                    FacebookUrl = new Uri(f.Internet.UrlWithPath("facebook")),
                    InstagramUrl = new Uri(f.Internet.UrlWithPath("instagram")),
                    BusinessAddress = businessAddressPool.GetNextNullable(),
                    BusinessEmailAddress = businessEmailPool.GetNextNullable()?.Value,
                    PhoneNumbers = [.. new[] { phoneNumberPool.GetNext(), phoneNumberPool.GetNext() }.DistinctBy(p => p.PhoneNumberType)],
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<SponsorDetailDto> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}