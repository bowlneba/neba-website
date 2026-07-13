using Neba.Api.Contracts.News.GetArticle;
using Neba.Api.Features.News.Domain;

namespace Neba.TestFactory.News;

public static class ArticleDetailResponseFactory
{
    public const string ValidSlug = "neba-fall-2025-tournament-recap";
    public const string ValidTitle = "NEBA Fall 2025 Tournament Recap";
    public const string ValidContent = "The fall 2025 season concluded with outstanding performances across all divisions.";
    public static readonly DateTimeOffset ValidPublishDateUtc = new(2025, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static ArticleDetailResponse Create(
        string? articleId = null,
        string? slug = null,
        PublicationStatus? publicationStatus = null,
        string? title = null,
        string? content = null,
        Uri? headerImageUrl = null,
        string? headerImageContainer = null,
        string? headerImagePath = null,
        string? headerImageContentType = null,
        long? headerImageSizeInBytes = null,
        DateTimeOffset? publishDateUtc = null,
        string? tournamentId = null,
        IReadOnlyCollection<ArticleAttachmentResponse>? attachments = null)
        => new()
        {
            ArticleId = articleId ?? Ulid.NewUlid().ToString(),
            Slug = slug ?? ValidSlug,
            PublicationStatus = publicationStatus?.Name ?? PublicationStatus.Published.Name,
            Title = title ?? ValidTitle,
            Content = content ?? ValidContent,
            HeaderImageUrl = headerImageUrl,
            HeaderImageContainer = headerImageContainer,
            HeaderImagePath = headerImagePath,
            HeaderImageContentType = headerImageContentType,
            HeaderImageSizeInBytes = headerImageSizeInBytes,
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
                ArticleId = Ulid.BogusString(faker),
                Slug = string.Join("-", faker.Lorem.Words(4)),
                PublicationStatus = faker.PickRandom(PublicationStatus.List.ToArray()).Name,
                Title = faker.Random.Words(4),
                Content = faker.Lorem.Paragraphs(2),
                HeaderImageUrl = hasHeaderImage ? new Uri(faker.Internet.Url()) : null,
                HeaderImageContainer = hasHeaderImage ? faker.System.CommonFileName() : null,
                HeaderImagePath = hasHeaderImage ? faker.System.FilePath() : null,
                HeaderImageContentType = hasHeaderImage ? faker.System.MimeType() : null,
                HeaderImageSizeInBytes = hasHeaderImage ? faker.Random.Long(1, 10_000_000) : null,
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