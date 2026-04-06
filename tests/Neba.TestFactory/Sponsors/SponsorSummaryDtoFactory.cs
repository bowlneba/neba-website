using Bogus;

using Neba.Application.Sponsors;
using Neba.Domain.Sponsors;

namespace Neba.TestFactory.Sponsors;

public static class SponsorSummaryDtoFactory
{
    public const string ValidName = "Some Sponsor, LLC";
    public const string ValidSlug = "some-sponsor";
    public const string ValidLogoContainer = "sponsor-logos";
    public const string ValidLogoPath = "some-sponsor/logo.png";
    public const string ValidTagPhrase = "We sponsor things!";
    public const string ValidDescription = "Some Sponsor, LLC is a leading provider of sponsorship opportunities for events and organizations around the world.";
    
    public static SponsorSummaryDto Create(
        string? name = null,
        string? slug = null,
        Uri? logoUri = null,
        string? logoContainer = null,
        string? logoPath = null,
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
            LogoUri = logoUri,
            LogoContainer = logoContainer ?? ValidLogoContainer,
            LogoPath = logoPath ?? ValidLogoPath,
            IsCurrentSponsor = isCurrentSponsor ?? true,
            Priority = priority ?? 1,
            Tier = tier ?? SponsorTier.Standard,
            Category = category ?? SponsorCategory.Technology,
            TagPhrase = tagPhrase ?? ValidTagPhrase,
            Description = description ?? ValidDescription,
            WebsiteUrl = websiteUrl,
            FacebookUrl = facebookUrl,
            InstagramUrl = instagramUrl
        };

    public static IReadOnlyCollection<SponsorSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SponsorSummaryDto>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName(),
                Slug = f.Lorem.Slug(),
                LogoUri = new Uri(f.Image.PicsumUrl()),
                LogoContainer = ValidLogoContainer,
                LogoPath = $"{f.Lorem.Slug()}/logo.png",
                IsCurrentSponsor = f.Random.Bool(),
                Priority = f.Random.Int(1, 10),
                Tier = f.PickRandom(SponsorTier.List.ToArray()),
                Category = f.PickRandom(SponsorCategory.List.ToArray()),
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