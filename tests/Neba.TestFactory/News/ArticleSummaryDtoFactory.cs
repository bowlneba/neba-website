using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.ListArticles;

namespace Neba.TestFactory.News;

public static class ArticleSummaryDtoFactory
{
    public const string ValidSlug = "test-article";
    public const string ValidTitle = "Test Article";
    public const string ValidExcerpt = "A short preview of the article.";
    public static readonly DateTimeOffset ValidPublishDateUtc = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static ArticleSummaryDto Create(
        ArticleId? id = null,
        string? slug = null,
        PublicationStatus? publicationStatus = null,
        string? title = null,
        string? excerpt = null,
        Uri? headerImageUrl = null,
        DateTimeOffset? publishDateUtc = null)
        => new()
        {
            Id = id ?? ArticleId.New(),
            Slug = slug ?? ValidSlug,
            PublicationStatus = publicationStatus ?? PublicationStatus.Published,
            Title = title ?? ValidTitle,
            Excerpt = excerpt ?? ValidExcerpt,
            HeaderImageUrl = headerImageUrl,
            PublishDateUtc = publishDateUtc ?? ValidPublishDateUtc
        };

    internal static IReadOnlyCollection<ArticleSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var title = faker.Random.Words(3);
            var hasImage = faker.Random.Bool();
            return new ArticleSummaryDto
            {
                Id = new ArticleId(Ulid.BogusString(faker)),
#pragma warning disable CA1308
                Slug = title.ToLowerInvariant().Replace(' ', '-'),
#pragma warning restore CA1308
                PublicationStatus = faker.PickRandom(PublicationStatus.List.ToArray()),
                Title = title,
                Excerpt = faker.Lorem.Sentence(),
                HeaderImageUrl = hasImage ? new Uri(faker.Internet.Avatar()) : null,
                PublishDateUtc = faker.Date.PastOffset(2)
            };
        })];
    }

    public static IReadOnlyCollection<ArticleSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}