using Bogus;

using Neba.Api.Features.News.ListArticles;

namespace Neba.TestFactory.News;

public static class ArticleSummaryDtoFactory
{
    public const string ValidSlug = "test-article";
    public const string ValidTitle = "Test Article";
    public const string ValidExcerpt = "A short preview of the article.";
    public static readonly DateTimeOffset ValidPublishDateUtc = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static ArticleSummaryDto Create(
        string? slug = null,
        string? title = null,
        string? excerpt = null,
        string? headerImageContainer = null,
        string? headerImagePath = null,
        Uri? headerImageUrl = null,
        DateTimeOffset? publishDateUtc = null)
        => new()
        {
            Slug = slug ?? ValidSlug,
            Title = title ?? ValidTitle,
            Excerpt = excerpt ?? ValidExcerpt,
            HeaderImageContainer = headerImageContainer,
            HeaderImagePath = headerImagePath,
            HeaderImageUrl = headerImageUrl,
            PublishDateUtc = publishDateUtc ?? ValidPublishDateUtc
        };

    public static IReadOnlyCollection<ArticleSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ArticleSummaryDto>()
            .CustomInstantiator(f =>
            {
                var title = f.Random.Words(3);
                var hasImage = f.Random.Bool();

                return new ArticleSummaryDto
                {
#pragma warning disable CA1308
                    Slug = title.ToLowerInvariant().Replace(' ', '-'),
#pragma warning restore CA1308
                    Title = title,
                    Excerpt = f.Lorem.Sentence(),
                    HeaderImageContainer = hasImage ? $"container-{f.Random.AlphaNumeric(8)}" : null,
                    HeaderImagePath = hasImage ? f.System.FileName() : null,
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
}
