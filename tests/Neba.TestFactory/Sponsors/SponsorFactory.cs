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

#pragma warning disable S107
    public static Sponsor Create(
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
    {
        var result = Sponsor.Create(
            name: name ?? ValidName,
            isCurrentSponsor: isCurrentSponsor ?? ValidIsCurrentSponsor,
            priority: priority ?? ValidPriority,
            tier: tier ?? ValidTier,
            category: category ?? ValidCategory,
            isTitleSponsorshipAvailable: true,
            slug: slug ?? ValidSlug,
            logo: logo,
            websiteUrl: websiteUrl,
            tagPhrase: tagPhrase,
            description: description,
            liveReadText: liveReadText,
            promotionalNotes: promotionalNotes,
            facebookUrl: facebookUrl,
            instagramUrl: instagramUrl,
            businessAddress: businessAddress,
            businessEmail: businessEmail,
            phoneNumbers: phoneNumbers,
            sponsorContact: sponsorContact);

        return result.IsError 
            ? throw new InvalidOperationException($"Failed to create sponsor: {result.Errors[0].Description}") 
            : result.Value;

    }
#pragma warning restore S107

    internal static IReadOnlyCollection<Sponsor> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var logoPool = UniquePool.CreateNullable(StoredFileFactory.Bogus(count * 10, faker), poolSeed);
        var businessAddressPool = UniquePool.CreateNullable(AddressFactory.BogusUs(count * 10, faker), poolSeed);
        var businessEmailPool = UniquePool.CreateNullable(EmailAddressFactory.Bogus(count * 10, faker), poolSeed);
        var phoneNumberPool = UniquePool.Create(PhoneNumberFactory.Bogus(count * 10, faker), poolSeed);
        var contactInfoPool = UniquePool.CreateNullable(ContactInfoFactory.Bogus(count * 10, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var result = Sponsor.Create(
                name: faker.Company.CompanyName(),
                isCurrentSponsor: faker.Random.Bool(),
                priority: faker.Random.Int(1, 10),
                tier: faker.PickRandom(SponsorTier.List.ToArray()),
                category: faker.PickRandom(SponsorCategory.List.ToArray()),
                isTitleSponsorshipAvailable: true,
                slug: faker.Lorem.Slug(),
                logo: logoPool.GetNextNullable(),
                websiteUrl: new Uri(faker.Internet.Url()),
                tagPhrase: faker.Company.CatchPhrase(),
                description: faker.Company.Bs(),
                liveReadText: faker.Lorem.Sentences(2),
                promotionalNotes: faker.Lorem.Sentences(3),
                facebookUrl: new Uri(faker.Internet.UrlWithPath("facebook")),
                instagramUrl: new Uri(faker.Internet.UrlWithPath("instagram")),
                businessAddress: businessAddressPool.GetNextNullable(),
                businessEmail: businessEmailPool.GetNextNullable(),
                phoneNumbers: [.. new[] { phoneNumberPool.GetNext(), phoneNumberPool.GetNext() }.DistinctBy(p => p.Type)],
                sponsorContact: contactInfoPool.GetNextNullable());

            return result.IsError 
                ? throw new InvalidOperationException($"Failed to create sponsor: {result.Errors[0].Description}") 
                : result.Value;

        })];
    }

    public static IReadOnlyCollection<Sponsor> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}