using Neba.Website.Server.Tournaments.Detail;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailArticleViewModelFactory
{
    public const string ValidTitle = "NEBA Singles Recap";
    public const string ValidSlug = "neba-singles-recap";

    public static TournamentDetailArticleViewModel Create(
        string? title = null,
        string? slug = null)
        => new()
        {
            Title = title ?? ValidTitle,
            Slug = slug ?? ValidSlug,
        };

    public static IReadOnlyCollection<TournamentDetailArticleViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailArticleViewModel>()
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
}
