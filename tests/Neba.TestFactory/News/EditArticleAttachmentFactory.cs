using Neba.Api.Features.News.EditArticle;
using Neba.Api.Features.Storage.Domain;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.News;

internal static class EditArticleAttachmentFactory
{
    public const string ValidDisplayName = "Test Attachment";
    public const bool ValidIsInline = false;

    public static EditArticleAttachment Create(
        string? displayName = null,
        bool? isInline = null,
        StoredFile? file = null)
        => new()
        {
            DisplayName = displayName ?? ValidDisplayName,
            IsInline = isInline ?? ValidIsInline,
            File = file ?? StoredFileFactory.Create()
        };

    internal static IReadOnlyCollection<EditArticleAttachment> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var files = new Queue<StoredFile>(StoredFileFactory.Bogus(count, faker));

        return [.. Enumerable.Range(0, count).Select(_ => new EditArticleAttachment
        {
            DisplayName = faker.Random.Words(2),
            IsInline = faker.Random.Bool(),
            File = files.Dequeue()
        })];
    }

    public static IReadOnlyCollection<EditArticleAttachment> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
