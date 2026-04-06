using Bogus;

using Neba.Domain.Contact;
using Neba.Domain.Sponsors;
using Neba.Domain.Storage;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.Sponsors;

public static class SponsorFactory
{
    public const string ValidName = "Joe's Sponsorship Company";
    public const string ValidSlug = "joes-sponsorship-company";

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
                IsCurrentSponsor = isCurrentSponsor ?? true,
                Priority = priority ?? 1,
                Tier = tier ?? SponsorTier.Standard,
                Category = category ?? SponsorCategory.Technology,
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

    public static IReadOnlyCollection<Sponsor> Bogus(int count, int? seed = null)
    {
        var logoPool = UniquePool.CreateNullable(StoredFileFactory.Bogus(count * 10, seed), seed);

        var faker = new Faker<Sponsor>()
            .CustomInstantiator(f => new()
            {
                Id = new SponsorId(Ulid.Bogus(f)),
                Name = f.Company.CompanyName(),
                Slug = f.Lorem.Slug(),
                IsCurrentSponsor = f.Random.Bool(),
                Priority = f.Random.Int(1, 10),
                Tier = f.PickRandom(SponsorTier.List.ToArray()),
                Category = f.PickRandom(SponsorCategory.List.ToArray()),
                Logo = logoPool.GetNextNullable(),
                WebsiteUrl = new Uri(f.Internet.Url()),
                TagPhrase = f.Company.CatchPhrase(),
                Description = f.Company.Bs(),
                LiveReadText = f.Lorem.Sentences(2),
                PromotionalNotes = f.Lorem.Sentences(3),
                FacebookUrl = new Uri(f.Internet.UrlWithPath("facebook")),
                InstagramUrl = new Uri(f.Internet.UrlWithPath("instagram")),
                BusinessAddress = null,
                BusinessEmail = null,
                PhoneNumbers = [],
                SponsorContact = null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}