using Neba.Api.Contracts.News.GetArticle;

namespace Neba.TestFactory.News;

public static class ArticleDetailResponseFactory
{
    public const string ValidSlug = "neba-fall-2025-tournament-recap";
    public const string ValidTitle = "NEBA Fall 2025 Tournament Recap";
    public const string ValidContent = "The fall 2025 season concluded with outstanding performances across all divisions.";
    public static readonly DateTimeOffset ValidPublishDateUtc = new(2025, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static ArticleDetailResponse Create(
        string? slug = null,
        string? title = null,
        string? content = null,
        Uri? headerImageUrl = null,
        DateTimeOffset? publishDateUtc = null,
        string? tournamentId = null,
        IReadOnlyCollection<ArticleAttachmentResponse>? attachments = null)
        => new()
        {
            Slug = slug ?? ValidSlug,
            Title = title ?? ValidTitle,
            Content = content ?? ValidContent,
            HeaderImageUrl = headerImageUrl,
            PublishDateUtc = publishDateUtc ?? ValidPublishDateUtc,
            TournamentId = tournamentId,
            Attachments = attachments ?? [],
        };

    internal static IReadOnlyCollection<ArticleDetailResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var hasHeaderImage = faker.Random.Bool();
            return new ArticleDetailResponse
            {
                Slug = string.Join("-", faker.Lorem.Words(4)),
                Title = faker.Random.Words(4),
                Content = faker.Lorem.Paragraphs(2),
                HeaderImageUrl = hasHeaderImage ? new Uri(faker.Internet.Url()) : null,
                PublishDateUtc = faker.Date.PastOffset(2),
                TournamentId = faker.Random.Bool() ? Ulid.BogusString(faker) : null,
                Attachments = ArticleAttachmentResponseFactory.Bogus(faker.Random.Int(0, 3), faker),
            };
        })];
    }

    public static IReadOnlyCollection<ArticleDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}