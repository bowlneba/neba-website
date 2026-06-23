using Bogus;

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

    public static IReadOnlyCollection<ArticleDetailDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ArticleDetailDto>()
            .CustomInstantiator(f =>
            {
                var hasHeaderImage = f.Random.Bool();
                return new ArticleDetailDto
                {
                    Slug = string.Join("-", f.Lorem.Words(4)),
                    Title = f.Random.Words(4),
                    Content = f.Lorem.Paragraphs(2),
                    HeaderImageUrl = hasHeaderImage ? new Uri(f.Internet.Url()) : null,
                    PublishDateUtc = f.Date.PastOffset(2),
                    Attachments = ArticleAttachmentDtoFactory.Bogus(f.Random.Int(0, 3), f),
                    TournamentId = f.Random.Bool() ? new TournamentId(Ulid.BogusString(f)) : null,
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<ArticleDetailDto> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}