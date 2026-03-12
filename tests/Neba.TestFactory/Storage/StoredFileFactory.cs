using Bogus;

using Neba.Domain.Storage;

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

    public static StoredFile Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<StoredFile> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<StoredFile>()
            .CustomInstantiator(f => new()
            {
                Container = $"container-{f.Random.AlphaNumeric(8)}",
                Path = $"{f.System.FileName()}.{f.System.CommonFileExt()}",
                ContentType = f.System.MimeType(),
                SizeInBytes = f.Random.Long(1, 10_000_000)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}