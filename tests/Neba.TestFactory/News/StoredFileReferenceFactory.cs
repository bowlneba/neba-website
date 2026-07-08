using Neba.Api.Features.News.DeleteArticle;

namespace Neba.TestFactory.News;

public static class StoredFileReferenceFactory
{
    public const string ValidContainer = "test-container";
    public const string ValidPath = "test-file.txt";

    public static StoredFileReference Create(
        string? container = null,
        string? path = null)
        => new()
        {
            Container = container ?? ValidContainer,
            Path = path ?? ValidPath
        };

    internal static IReadOnlyCollection<StoredFileReference> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new StoredFileReference
        {
            Container = $"container-{faker.Random.AlphaNumeric(8)}",
            Path = $"{faker.System.FileName()}.{faker.System.CommonFileExt()}"
        })];
    }

    public static IReadOnlyCollection<StoredFileReference> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
