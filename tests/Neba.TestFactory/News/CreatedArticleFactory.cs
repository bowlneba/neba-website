using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.News.Domain;

namespace Neba.TestFactory.News;

public static class CreatedArticleFactory
{
    public const string ValidSlug = "neba-fall-2025-tournament-recap";

    public static CreatedArticle Create(
        ArticleId? id = null,
        string? slug = null)
        => new()
        {
            Id = id ?? ArticleId.New(),
            Slug = slug ?? ValidSlug,
        };

    internal static IReadOnlyCollection<CreatedArticle> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new CreatedArticle
        {
            Id = new ArticleId(Ulid.BogusString(faker)),
            Slug = string.Join("-", faker.Lorem.Words(4)),
        })];
    }

    public static IReadOnlyCollection<CreatedArticle> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
