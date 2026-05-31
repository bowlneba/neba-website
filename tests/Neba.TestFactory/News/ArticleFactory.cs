using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.News;

public static class ArticleFactory
{
    public const string ValidTitle = "Test Article";
    public const string ValidSlug = "test-article";
    public const string ValidContent = "Test content.";
    public static readonly PublicationStatus ValidPublicationStatus = PublicationStatus.Draft;
    public static readonly DateTimeOffset ValidPublishDateUtc = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

#pragma warning disable S107
    public static Article Create(
        ArticleId? id = null,
        string? title = null,
        string? slug = null,
        string? content = null,
        PublicationStatus? publicationStatus = null,
        DateTimeOffset? publishDateUtc = null,
        StoredFile? headerImage = null,
        TournamentId? tournamentId = null,
        IReadOnlyCollection<ArticleAttachment>? attachments = null)
    {
        var article = new Article
        {
            Id = id ?? ArticleId.New(),
            Title = title ?? ValidTitle,
            Slug = slug ?? ValidSlug,
            Content = content ?? ValidContent,
            PublicationStatus = publicationStatus ?? ValidPublicationStatus,
            PublishDateUtc = publishDateUtc ?? ValidPublishDateUtc,
            HeaderImage = headerImage,
            TournamentId = tournamentId ?? TournamentId.New()
        };

        foreach (var attachment in attachments ?? [])
        {
            var result = article.AddAttachment(attachment.DisplayName, attachment.File, attachment.IsInline);

            if (result.IsError)
            {
                throw new InvalidOperationException($"Failed to add attachment '{attachment.DisplayName}': {result.Errors[0].Description}");
            }
        }

        return article;
    }
#pragma warning restore S107

#pragma warning disable CA1308
    public static IReadOnlyCollection<Article> Bogus(int count, int? seed = null)
    {
        var uniqueImages = UniquePool.CreateNullable(StoredFileFactory.Bogus(count, seed), seed);

        var faker = new Faker<Article>()
            .CustomInstantiator(f =>
            {
                var title = f.Random.Words(3);

                var article = new Article
                {
                    Id = new ArticleId(Ulid.BogusString(f)),
                    Title = title,
                    Slug = title.ToLowerInvariant().Replace(' ', '-'),
                    Content = f.Lorem.Paragraphs(2),
                    PublicationStatus = f.PickRandom(PublicationStatus.List.ToArray()),
                    PublishDateUtc = f.Date.PastOffset(2),
                    HeaderImage = uniqueImages.GetNextNullable(),
                    TournamentId = new TournamentId(Ulid.BogusString(f))
                };

                var attachmentCount = f.Random.Int(0, 3);
                for (var i = 0; i < attachmentCount; i++)
                {
                    article.AddAttachment(
                        f.Random.Words(2),
                        new StoredFile
                        {
                            Container = $"container-{f.Random.AlphaNumeric(8)}",
                            Path = f.System.FileName(),
                            ContentType = f.System.MimeType(),
                            SizeInBytes = f.Random.Long(1, 10_000_000)
                        },
                        f.Random.Bool());
                }

                return article;
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
#pragma warning restore CA1308
}