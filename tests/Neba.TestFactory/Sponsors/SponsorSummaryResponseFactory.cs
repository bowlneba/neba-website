using Bogus;

using Neba.Api.Contracts.Sponsors;
using Neba.Application.Sponsors;
using Neba.Domain.Sponsors;
using Neba.Website.Server.Sponsors;

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
            IsCurrentSponsor = isCurrentSponsor ?? true,
            Priority = priority ?? 1,
            Tier = tier?.Name ?? SponsorTier.Standard.Name,
            Category = category?.Name ?? SponsorCategory.Technology.Name,
            TagPhrase = tagPhrase ?? ValidTagPhrase,
            Description = description ?? ValidDescription,
            WebsiteUrl = websiteUrl,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl
        };

    public static IReadOnlyCollection<SponsorSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SponsorSummaryResponse>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName(),
                Slug = f.Lorem.Slug(),
                LogoUrl = new Uri(f.Image.PicsumUrl()),
                IsCurrentSponsor = f.Random.Bool(),
                Priority = f.Random.Int(1, 10),
                Tier = f.PickRandom(SponsorTier.List.ToArray()).Name,
                Category = f.PickRandom(SponsorCategory.List.ToArray()).Name,
                TagPhrase = f.Company.CatchPhrase(),
                Description = f.Lorem.Sentence(2),
                WebsiteUrl = new Uri(f.Internet.Url()),
                FacebookUrl = new Uri(f.Internet.UrlWithPath("facebook")),
                InstagramUrl = new Uri(f.Internet.UrlWithPath("instagram"))
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}