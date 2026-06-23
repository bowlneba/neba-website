using Neba.Api.Features.Storage.Domain;

namespace Neba.TestFactory.Storage;

public static class StoredFileFactory
{
    public const string ValidContainer = "test-container";
    public const string ValidPath = "test-file.txt";
    public const string ValidContentType = "text/plain";
    public const long ValidSizeInBytes = 1024;

    public static StoredFile Create(
        string? container = null,
        string? path = null,
        string? contentType = null,
        long? sizeInBytes = null)
    {
        return new StoredFile
        {
            Container = container ?? ValidContainer,
            Path = path ?? ValidPath,
            ContentType = contentType ?? ValidContentType,
            SizeInBytes = sizeInBytes ?? ValidSizeInBytes
        };
    }

    public static IReadOnlyCollection<StoredFile> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new StoredFile
        {
            Container = $"container-{faker.Random.AlphaNumeric(8)}",
            Path = $"{faker.System.FileName()}.{faker.System.CommonFileExt()}",
            ContentType = faker.System.MimeType(),
            SizeInBytes = faker.Random.Long(1, 10_000_000)
        })];
    }

    public static IReadOnlyCollection<StoredFile> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}