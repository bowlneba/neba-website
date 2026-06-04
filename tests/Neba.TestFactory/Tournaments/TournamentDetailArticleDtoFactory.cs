using Neba.Api.Features.Tournaments.GetTournament;

namespace Neba.TestFactory.Tournaments;

public static class TournamentDetailArticleDtoFactory
{
    public const string ValidTitle = "NEBA Singles Recap";
    public const string ValidSlug = "neba-singles-recap";

    public static TournamentDetailArticleDto Create(
        string? title = null,
        string? slug = null)
        => new()
        {
            Title = title ?? ValidTitle,
            Slug = slug ?? ValidSlug,
        };

    public static IReadOnlyCollection<TournamentDetailArticleDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<TournamentDetailArticleDto>()
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
