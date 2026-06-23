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

    public static IReadOnlyCollection<ArticleSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ArticleSummaryResponse>()
            .CustomInstantiator(f =>
            {
                var title = f.Random.Words(3);
                var hasImage = f.Random.Bool();

                return new ArticleSummaryResponse
                {
#pragma warning disable CA1308
                    Slug = title.ToLowerInvariant().Replace(' ', '-'),
#pragma warning restore CA1308
                    Title = title,
                    Excerpt = f.Lorem.Sentence(),
                    HeaderImageUrl = hasImage ? new Uri(f.Internet.Avatar()) : null,
                    PublishDateUtc = f.Date.PastOffset(2)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<ArticleSummaryResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}