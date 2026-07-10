using Neba.Api.Contracts.News.CreateArticle;

namespace Neba.TestFactory.News;

public static class ArticleResponseFactory
{
    public const string ValidSlug = "test-article";

    public static ArticleResponse Create(
        string? articleId = null,
        string? slug = null)
        => new()
        {
            ArticleId = articleId ?? Ulid.NewUlid().ToString(),
            Slug = slug ?? ValidSlug
        };

    internal static IReadOnlyCollection<ArticleResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var title = faker.Random.Words(3);
            return new ArticleResponse
            {
                ArticleId = Ulid.BogusString(faker),
#pragma warning disable CA1308
                Slug = title.ToLowerInvariant().Replace(' ', '-'),
#pragma warning restore CA1308
            };
        })];
    }

    public static IReadOnlyCollection<ArticleResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
