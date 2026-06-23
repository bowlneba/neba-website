using Neba.Api.Features.News.GetArticle;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.News;

public static class ArticleDetailDtoFactory
{
    public const string ValidSlug = "neba-fall-2025-tournament-recap";
    public const string ValidTitle = "NEBA Fall 2025 Tournament Recap";
    public const string ValidContent = "The fall 2025 season concluded with outstanding performances across all divisions.";
    public static readonly DateTimeOffset ValidPublishDateUtc = new(2025, 10, 1, 12, 0, 0, TimeSpan.Zero);

    public static ArticleDetailDto Create(
        string? slug = null,
        string? title = null,
        string? content = null,
        Uri? headerImageUrl = null,
        DateTimeOffset? publishDateUtc = null,
        IReadOnlyCollection<ArticleAttachmentDto>? attachments = null,
        TournamentId? tournamentId = null)
        => new()
        {
            Slug = slug ?? ValidSlug,
            Title = title ?? ValidTitle,
            Content = content ?? ValidContent,
            HeaderImageUrl = headerImageUrl,
            PublishDateUtc = publishDateUtc ?? ValidPublishDateUtc,
            Attachments = attachments ?? [],
            TournamentId = tournamentId,
        };

    public static IReadOnlyCollection<ArticleDetailDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var hasHeaderImage = faker.Random.Bool();
            return new ArticleDetailDto
            {
                Slug = string.Join("-", faker.Lorem.Words(4)),
                Title = faker.Random.Words(4),
                Content = faker.Lorem.Paragraphs(2),
                HeaderImageUrl = hasHeaderImage ? new Uri(faker.Internet.Url()) : null,
                PublishDateUtc = faker.Date.PastOffset(2),
                Attachments = ArticleAttachmentDtoFactory.Bogus(faker.Random.Int(0, 3), faker),
                TournamentId = faker.Random.Bool() ? new TournamentId(Ulid.BogusString(faker)) : null,
            };
        })];
    }

    public static IReadOnlyCollection<ArticleDetailDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}