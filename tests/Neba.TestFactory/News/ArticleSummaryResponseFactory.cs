using Neba.Api.Contracts.News.ListArticles;

namespace Neba.TestFactory.News;

public static class ArticleSummaryResponseFactory
{
    public const string ValidSlug = "test-article";
    public const string ValidTitle = "Test Article";
    public const string ValidExcerpt = "A short preview of the article.";
    public static readonly DateTimeOffset ValidPublishDateUtc = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static ArticleSummaryResponse Create(
        string? slug = null,
        string? title = null,
        string? excerpt = null,
        Uri? headerImageUrl = null,
        DateTimeOffset? publishDateUtc = null)
        => new()
        {
            Slug = slug ?? ValidSlug,
            Title = title ?? ValidTitle,
            Excerpt = excerpt ?? ValidExcerpt,
            HeaderImageUrl = headerImageUrl,
            PublishDateUtc = publishDateUtc ?? ValidPublishDateUtc
        };

    internal static IReadOnlyCollection<ArticleSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var title = faker.Random.Words(3);
            var hasImage = faker.Random.Bool();
            return new ArticleSummaryResponse
            {
#pragma warning disable CA1308
                Slug = title.ToLowerInvariant().Replace(' ', '-'),
#pragma warning restore CA1308
                Title = title,
                Excerpt = faker.Lorem.Sentence(),
                HeaderImageUrl = hasImage ? new Uri(faker.Internet.Avatar()) : null,
                PublishDateUtc = faker.Date.PastOffset(2)
            };
        })];
    }

    public static IReadOnlyCollection<ArticleSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}