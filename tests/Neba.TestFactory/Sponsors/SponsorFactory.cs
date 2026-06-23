using Neba.Api.Contacts.Domain;
using Neba.Api.Features.Sponsors.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.Sponsors;

public static class SponsorFactory
{
    public const string ValidName = "Joe's Sponsorship Company";
    public const string ValidSlug = "joes-sponsorship-company";
    public const bool ValidIsCurrentSponsor = true;
    public const int ValidPriority = 1;
    public static readonly SponsorTier ValidTier = SponsorTier.Standard;
    public static readonly SponsorCategory ValidCategory = SponsorCategory.Technology;

    public static Sponsor Create(
        SponsorId? id = null,
        string? name = null,
        string? slug = null,
        bool? isCurrentSponsor = null,
        int? priority = null,
        SponsorTier? tier = null,
        SponsorCategory? category = null,
        StoredFile? logo = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        string? description = null,
        string? liveReadText = null,
        string? promotionalNotes = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        Address? businessAddress = null,
        EmailAddress? businessEmail = null,
        IReadOnlyCollection<PhoneNumber>? phoneNumbers = null,
        ContactInfo? sponsorContact = null)
            => new()
            {
                Id = id ?? SponsorId.New(),
                Name = name ?? ValidName,
                Slug = slug ?? ValidSlug,
                IsCurrentSponsor = isCurrentSponsor ?? ValidIsCurrentSponsor,
                Priority = priority ?? ValidPriority,
                Tier = tier ?? ValidTier,
                Category = category ?? ValidCategory,
                Logo = logo,
                WebsiteUrl = websiteUrl,
                TagPhrase = tagPhrase,
                Description = description,
                LiveReadText = liveReadText,
                PromotionalNotes = promotionalNotes,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                BusinessAddress = businessAddress,
                BusinessEmail = businessEmail,
                PhoneNumbers = phoneNumbers ?? [],
                SponsorContact = sponsorContact
            };

    public static IReadOnlyCollection<Sponsor> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var logoPool = UniquePool.CreateNullable(StoredFileFactory.Bogus(count * 10, faker), poolSeed);
        var businessAddressPool = UniquePool.CreateNullable(AddressFactory.BogusUs(count * 10, faker), poolSeed);
        var businessEmailPool = UniquePool.CreateNullable(EmailAddressFactory.Bogus(count * 10, faker), poolSeed);
        var phoneNumberPool = UniquePool.Create(PhoneNumberFactory.Bogus(count * 10, faker), poolSeed);
        var contactInfoPool = UniquePool.CreateNullable(ContactInfoFactory.Bogus(count * 10, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new Sponsor
        {
            Id = new SponsorId(Ulid.BogusString(faker)),
            Name = faker.Company.CompanyName(),
            Slug = faker.Lorem.Slug(),
            IsCurrentSponsor = faker.Random.Bool(),
            Priority = faker.Random.Int(1, 10),
            Tier = faker.PickRandom(SponsorTier.List.ToArray()),
            Category = faker.PickRandom(SponsorCategory.List.ToArray()),
            Logo = logoPool.GetNextNullable(),
            WebsiteUrl = new Uri(faker.Internet.Url()),
            TagPhrase = faker.Company.CatchPhrase(),
            Description = faker.Company.Bs(),
            LiveReadText = faker.Lorem.Sentences(2),
            PromotionalNotes = faker.Lorem.Sentences(3),
            FacebookUrl = new Uri(faker.Internet.UrlWithPath("facebook")),
            InstagramUrl = new Uri(faker.Internet.UrlWithPath("instagram")),
            BusinessAddress = businessAddressPool.GetNextNullable(),
            BusinessEmail = businessEmailPool.GetNextNullable(),
            PhoneNumbers = [.. new[] { phoneNumberPool.GetNext(), phoneNumberPool.GetNext() }.DistinctBy(p => p.Type)],
            SponsorContact = contactInfoPool.GetNextNullable()
        })];
    }

    public static IReadOnlyCollection<Sponsor> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}