using Neba.Api.Contracts.Sponsors;
using Neba.Api.Features.Sponsors.Domain;

namespace Neba.TestFactory.Sponsors;

public static class SponsorSummaryResponseFactory
{
    public const string ValidName = "Some Sponsor, LLC";
    public const string ValidSlug = "some-sponsor";
    public const string ValidLogoContainer = "sponsor-logos";
    public const string ValidLogoPath = "some-sponsor/logo.png";
    public const string ValidTagPhrase = "We sponsor things!";
    public const string ValidDescription = "Some Sponsor, LLC is a leading provider of sponsorship opportunities for events and organizations around the world.";

    public static SponsorSummaryResponse Create(
        string? name = null,
        string? slug = null,
        Uri? logoUrl = null,
        bool? isCurrentSponsor = null,
        int? priority = null,
        SponsorTier? tier = null,
        SponsorCategory? category = null,
        string? tagPhrase = null,
        string? description = null,
        Uri? websiteUrl = null,
        Uri? facebookUrl = null,
        Uri? instagramUrl = null
    )
        => new()
        {
            Name = name ?? ValidName,
            Slug = slug ?? ValidSlug,
            LogoUrl = logoUrl,
            IsCurrentSponsor = isCurrentSponsor ?? SponsorFactory.ValidIsCurrentSponsor,
            Priority = priority ?? SponsorFactory.ValidPriority,
            Tier = tier?.Name ?? SponsorFactory.ValidTier.Name,
            Category = category?.Name ?? SponsorFactory.ValidCategory.Name,
            TagPhrase = tagPhrase ?? ValidTagPhrase,
            Description = description ?? ValidDescription,
            WebsiteUrl = websiteUrl,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl
        };

    public static IReadOnlyCollection<SponsorSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SponsorSummaryResponse
        {
            Name = faker.Company.CompanyName(),
            Slug = faker.Lorem.Slug(),
            LogoUrl = new Uri(faker.Image.PicsumUrl()),
            IsCurrentSponsor = faker.Random.Bool(),
            Priority = faker.Random.Int(1, 10),
            Tier = faker.PickRandom(SponsorTier.List.ToArray()).Name,
            Category = faker.PickRandom(SponsorCategory.List.ToArray()).Name,
            TagPhrase = faker.Company.CatchPhrase(),
            Description = faker.Lorem.Sentence(2),
            WebsiteUrl = new Uri(faker.Internet.Url()),
            FacebookUrl = new Uri(faker.Internet.UrlWithPath("facebook")),
            InstagramUrl = new Uri(faker.Internet.UrlWithPath("instagram"))
        })];
    }

    public static IReadOnlyCollection<SponsorSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}