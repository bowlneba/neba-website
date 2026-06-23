using Neba.Api.Contracts.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailArticleResponseFactory
{
    public const string ValidTitle = "NEBA Singles Recap";
    public const string ValidSlug = "neba-singles-recap";

    public static TournamentDetailArticleResponse Create(
        string? title = null,
        string? slug = null)
        => new()
        {
            Title = title ?? ValidTitle,
            Slug = slug ?? ValidSlug,
        };

    public static IReadOnlyCollection<TournamentDetailArticleResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailArticleResponse>()
            .CustomInstantiator(f => new()
            {
                Title = f.Lorem.Sentence(4),
                Slug = f.Internet.DomainWord() + "-" + f.Internet.DomainWord(),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<TournamentDetailArticleResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
