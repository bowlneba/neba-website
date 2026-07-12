using Bogus;

using Neba.Api.Contracts.Uploads;

namespace Neba.TestFactory.Uploads;

public static class UploadedFileResponseFactory
{
    public const string ValidContainer = "test-container";
    public const string ValidPath = "test-file.txt";
    public const string ValidFileName = "test-file.txt";
    public const string ValidContentType = "text/plain";
    public const long ValidSizeInBytes = 1024;
    public static readonly Uri ValidUrl = new("https://storage.example.com/test-container/test-file.txt");

    public static UploadedFileResponse Create(
        string? container = null,
        string? path = null,
        string? fileName = null,
        string? contentType = null,
        long? sizeInBytes = null,
        Uri? url = null)
        => new()
        {
            Container = container ?? ValidContainer,
            Path = path ?? ValidPath,
            FileName = fileName ?? ValidFileName,
            ContentType = contentType ?? ValidContentType,
            SizeInBytes = sizeInBytes ?? ValidSizeInBytes,
            Url = url ?? ValidUrl
        };

    internal static IReadOnlyCollection<UploadedFileResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var fileName = $"{faker.System.FileName()}.{faker.System.CommonFileExt()}";
            return new UploadedFileResponse
            {
                Container = $"container-{faker.Random.AlphaNumeric(8)}",
                Path = fileName,
                FileName = fileName,
                ContentType = faker.System.MimeType(),
                SizeInBytes = faker.Random.Long(1, 10_000_000),
                Url = new Uri(faker.Internet.Url())
            };
        })];
    }

    public static IReadOnlyCollection<UploadedFileResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}