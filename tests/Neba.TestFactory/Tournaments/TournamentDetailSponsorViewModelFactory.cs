using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailSponsorViewModelFactory
{
    public const string ValidName = "Acme Corp";
    public const string ValidSlug = "acme-corp";

    public static TournamentDetailSponsorViewModel Create(
        string? name = null,
        string? slug = null,
        Uri? logoUrl = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null)
        => new()
        {
            Name = name ?? ValidName,
            Slug = slug ?? ValidSlug,
            LogoUrl = logoUrl,
            WebsiteUrl = websiteUrl,
            TagPhrase = tagPhrase,
        };

    public static IReadOnlyCollection<TournamentDetailSponsorViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailSponsorViewModel>()
            .CustomInstantiator(f => new TournamentDetailSponsorViewModel
            {
                Name = f.Company.CompanyName(),
                Slug = f.Lorem.Slug(),
                LogoUrl = f.Random.Bool() ? new Uri(f.Internet.Avatar()) : null,
                WebsiteUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                TagPhrase = f.Random.Bool() ? f.Company.CatchPhrase() : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
