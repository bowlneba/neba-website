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
            TournamentId = tournamentId
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
    public static IReadOnlyCollection<Article> Bogus(int count, Faker faker, IEnumerable<TournamentId>? tournamentIds = null)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var uniqueImages = UniquePool.CreateNullable(StoredFileFactory.Bogus(count, faker), poolSeed);
        var uniqueTournamentIds = UniquePool.CreateNullable(tournamentIds ?? [], poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var title = faker.Random.Words(3);

            var article = new Article
            {
                Id = new ArticleId(Ulid.BogusString(faker)),
                Title = title,
                Slug = title.ToLowerInvariant().Replace(' ', '-'),
                Content = faker.Lorem.Paragraphs(2),
                PublicationStatus = faker.PickRandom(PublicationStatus.List.ToArray()),
                PublishDateUtc = faker.Date.PastOffset(2).ToUniversalTime(),
                HeaderImage = uniqueImages.GetNextNullable(),
                TournamentId = uniqueTournamentIds.GetNextNullable()
            };

            var attachmentCount = faker.Random.Int(0, 3);
            for (var i = 0; i < attachmentCount; i++)
            {
                article.AddAttachment(
                    faker.Random.Words(2),
                    new StoredFile
                    {
                        Container = $"container-{faker.Random.AlphaNumeric(8)}",
                        Path = faker.System.FileName(),
                        ContentType = faker.System.MimeType(),
                        SizeInBytes = faker.Random.Long(1, 10_000_000)
                    },
                    faker.Random.Bool());
            }

            return article;
        })];
    }
#pragma warning restore CA1308

    public static IReadOnlyCollection<Article> Bogus(int count, int? seed = null, IEnumerable<TournamentId>? tournamentIds = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker, tournamentIds);
    }
}