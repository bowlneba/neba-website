using Bogus;

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

    public static IReadOnlyCollection<ArticleDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ArticleDetailResponse>()
            .CustomInstantiator(f =>
            {
                var hasHeaderImage = f.Random.Bool();

                return new ArticleDetailResponse
                {
                    Slug = string.Join("-", f.Lorem.Words(4)),
                    Title = f.Random.Words(4),
                    Content = f.Lorem.Paragraphs(2),
                    HeaderImageUrl = hasHeaderImage ? new Uri(f.Internet.Url()) : null,
                    PublishDateUtc = f.Date.PastOffset(2),
                    TournamentId = f.Random.Bool() ? Ulid.NewUlid().ToString() : null,
                    Attachments = ArticleAttachmentResponseFactory.Bogus(f.Random.Int(0, 3), f),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<ArticleDetailResponse> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
