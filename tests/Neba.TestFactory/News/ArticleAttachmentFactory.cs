using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.News;

public static class ArticleAttachmentFactory
{
    public const string ValidDisplayName = "Test Attachment";
    public const bool ValidIsInline = false;

    public static ArticleAttachment Create(
        ArticleAttachmentId? id = null,
        string? displayName = null,
        StoredFile? file = null,
        bool? isInline = null)
        => new()
        {
            Id = id ?? ArticleAttachmentId.New(),
            DisplayName = displayName ?? ValidDisplayName,
            File = file ?? StoredFileFactory.Create(),
            IsInline = isInline ?? ValidIsInline
        };

    internal static IReadOnlyCollection<ArticleAttachment> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var files = new Queue<StoredFile>(StoredFileFactory.Bogus(count, faker));

        return [.. Enumerable.Range(0, count).Select(_ => new ArticleAttachment
        {
            Id = new ArticleAttachmentId(Ulid.BogusString(faker)),
            DisplayName = faker.Random.Words(2),
            File = files.Dequeue(),
            IsInline = faker.Random.Bool()
        })];
    }

    public static IReadOnlyCollection<ArticleAttachment> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}