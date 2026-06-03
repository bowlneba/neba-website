using Neba.Api.Features.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailSponsorDtoFactory
{
    public const string ValidName = "Test Sponsor";
    public const string ValidSlug = "test-sponsor";

    public static TournamentDetailSponsorDto Create(
        string? name = null,
        string? slug = null,
        Uri? websiteUrl = null,
        string? tagPhrase = null,
        Uri? logoUrl = null)
        => new()
        {
            Name = name ?? ValidName,
            Slug = slug ?? ValidSlug,
            WebsiteUrl = websiteUrl,
            TagPhrase = tagPhrase,
            LogoUrl = logoUrl,
        };

    public static IReadOnlyCollection<TournamentDetailSponsorDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailSponsorDto>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName(),
                Slug = f.Internet.DomainWord(),
                WebsiteUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
                TagPhrase = f.Random.Bool() ? f.Lorem.Sentence() : null,
                LogoUrl = f.Random.Bool() ? new Uri(f.Internet.Url()) : null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}