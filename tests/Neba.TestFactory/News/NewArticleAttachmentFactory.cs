using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.Storage.Domain;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.News;

internal static class NewArticleAttachmentFactory
{
    public const string ValidDisplayName = "Test Attachment";
    public const bool ValidIsInline = false;

    public static NewArticleAttachment Create(
        string? displayName = null,
        bool? isInline = null,
        StoredFile? file = null)
        => new()
        {
            DisplayName = displayName ?? ValidDisplayName,
            IsInline = isInline ?? ValidIsInline,
            File = file ?? StoredFileFactory.Create()
        };

    internal static IReadOnlyCollection<NewArticleAttachment> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var files = new Queue<StoredFile>(StoredFileFactory.Bogus(count, faker));

        return [.. Enumerable.Range(0, count).Select(_ => new NewArticleAttachment
        {
            DisplayName = faker.Random.Words(2),
            IsInline = faker.Random.Bool(),
            File = files.Dequeue()
        })];
    }

    public static IReadOnlyCollection<NewArticleAttachment> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
