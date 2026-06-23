using Bogus;

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

    public static IReadOnlyCollection<ArticleAttachment> Bogus(int count, int? seed = null)
    {
        var files = new Queue<StoredFile>(StoredFileFactory.Bogus(count, seed));

        var faker = new Faker<ArticleAttachment>()
            .CustomInstantiator(f => new()
            {
                Id = new ArticleAttachmentId(Ulid.BogusString(f)),
                DisplayName = f.Random.Words(2),
                File = files.Dequeue(),
                IsInline = f.Random.Bool()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<ArticleAttachment> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}