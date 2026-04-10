using Bogus;

using Neba.Application.Contact;
using Neba.Application.Sponsors.GetSponsorDetail;
using Neba.Domain.Sponsors;
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
        string? logoContainer = null,
        string? logoPath = null,
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
        ContactInfoDto? sponsorContact = null)
            => new()
            {
                Id = id ?? SponsorId.New(),
                Name = name ?? ValidName,
                Slug = slug ?? ValidSlug,
                IsCurrentSponsor = isCurrentSponsor ?? true,
                Priority = priority ?? 1,
                Tier = tier?.Name ?? SponsorTier.Standard.Name,
                Category = category?.Name ?? SponsorCategory.Technology.Name,
                LogoContainer = logoContainer,
                LogoPath = logoPath,
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
                SponsorContactInfo = sponsorContact
            };

    public static IReadOnlyCollection<SponsorDetailDto> Bogus(int count, int? seed = null)
    {
        var businessAddressPool = UniquePool.CreateNullable(AddressDtoFactory.Bogus(count * 10, seed), seed);
        var businessEmailPool = UniquePool.CreateNullable(EmailAddressFactory.Bogus(count * 10, seed), seed);
        var phoneNumberPool = UniquePool.Create(PhoneNumberDtoFactory.Bogus(count * 10, seed), seed);
        var contactInfoPool = UniquePool.CreateNullable(ContactInfoDtoFactory.Bogus(count * 10, seed), seed);

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
                    LogoContainer = hasLogo ? f.System.FileName() : null,
                    LogoPath = hasLogo ? f.System.FilePath() : null,
                    WebsiteUrl = new Uri(f.Internet.Url()),
                    TagPhrase = f.Company.CatchPhrase(),
                    Description = f.Company.Bs(),
                    LiveReadText = f.Lorem.Sentences(2),
                    PromotionalNotes = f.Lorem.Sentences(3),
                    FacebookUrl = new Uri(f.Internet.UrlWithPath("facebook")),
                    InstagramUrl = new Uri(f.Internet.UrlWithPath("instagram")),
                    BusinessAddress = businessAddressPool.GetNextNullable(),
                    BusinessEmailAddress = businessEmailPool.GetNextNullable()?.Value,
                    PhoneNumbers = [.. new[] { phoneNumberPool.GetNext(), phoneNumberPool.GetNext() }.DistinctBy(p => p.PhoneNumberType)],
                    SponsorContactInfo = contactInfoPool.GetNextNullable()
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}