using System.Globalization;

using Bogus;

using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.Sponsors;
using Neba.Domain.Contact;
using Neba.Domain.Sponsors;
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
    public const string ValidContactName = "Joe D. Sponsor";
    public const string ValidContactEmail = "joe@personal.com";
    public const string ValidContactPhoneNumber = "8005551234";
    public const string ValidPhoneNumberType = "M";

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
        string? promotionalNotes = null,
        string? liveReadText = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null,
        string? businessStreet = null,
        string? businessCity = null,
        string? businessState = null,
        string? businessPostalCode = null,
        string? businessCountry = null,
        string? businessEmailAddress = null,
        IReadOnlyCollection<PhoneNumberResponse>? phoneNumbers = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhoneNumber = null,
        string? contactPhoneNumberType = null)
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
                PromotionalNotes = promotionalNotes,
                LiveReadText = liveReadText,
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                BusinessStreet = businessStreet ?? ValidBusinessStreet,
                BusinessCity = businessCity ?? ValidBusinessCity,
                BusinessState = businessState ?? ValidBusinessState,
                BusinessPostalCode = businessPostalCode ?? ValidBusinessPostalCode,
                BusinessCountry = businessCountry ?? ValidBusinessCountry,
                BusinessEmailAddress = businessEmailAddress ?? ValidBusinessEmailAddress,
                PhoneNumbers = phoneNumbers ?? [PhoneNumberResponseFactory.Create()],
                SponsorContactName = contactName ?? ValidContactName,
                SponsorContactEmailAddress = contactEmail ?? ValidContactEmail,
                SponsorContactPhoneNumber = contactPhoneNumber ?? ValidContactPhoneNumber,
                SponsorContactPhoneNumberType = contactPhoneNumberType ?? ValidPhoneNumberType,
            };

    public static IReadOnlyCollection<SponsorDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SponsorDetailResponse>()
            .CustomInstantiator(f => new()
            {
                Id = Ulid.Bogus(f),
                Name = f.Company.CompanyName(),
                Slug = f.Lorem.Slug(),
                IsCurrentSponsor = f.Random.Bool(),
                Priority = f.Random.Int(1, 10),
                Tier = f.PickRandom(SponsorTier.List.ToArray()).Name,
                Category = f.PickRandom(SponsorCategory.List.ToArray()).Name,
                LogoUrl = new Uri(f.Internet.Avatar()),
                WebsiteUrl = new Uri(f.Internet.Url()),
                TagPhrase = f.Company.CatchPhrase(),
                Description = f.Company.Bs(),
                PromotionalNotes = f.Company.CompanyName() + " is a great sponsor!",
                LiveReadText = "Live read for " + f.Company.CompanyName(),
                FacebookUrl = new Uri(f.Internet.Url()),
                InstagramUrl = new Uri(f.Internet.Url()),
                BusinessStreet = f.Address.StreetAddress(),
                BusinessCity = f.Address.City(),
                BusinessState = f.Address.StateAbbr(),
                BusinessPostalCode = f.Address.ZipCode(),
                BusinessCountry = f.Address.CountryCode(),
                BusinessEmailAddress = f.Internet.Email(),
                PhoneNumbers = PhoneNumberResponseFactory.Bogus(2, seed),
                SponsorContactName = f.Name.FullName(),
                SponsorContactEmailAddress = f.Internet.Email(),
                SponsorContactPhoneNumber = f.Phone.PhoneNumber("##########"),
                SponsorContactPhoneNumberType = f.PickRandom(PhoneNumberType.List.ToArray()).Name,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}