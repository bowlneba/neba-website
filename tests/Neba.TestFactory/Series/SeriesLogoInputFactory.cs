using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.TestFactory.Series;

public static class SeriesLogoInputFactory
{
    public const string ValidContainer = "series-logos";
    public const string ValidPath = "vincent-hall.png";
    public const string ValidContentType = "image/png";
    public const long ValidSizeInBytes = 2048;

    public static SeriesLogoInput Create(
        string? container = null,
        string? path = null,
        string? contentType = null,
        long? sizeInBytes = null)
        => new()
        {
            Container = container ?? ValidContainer,
            Path = path ?? ValidPath,
            ContentType = contentType ?? ValidContentType,
            SizeInBytes = sizeInBytes ?? ValidSizeInBytes,
        };

    public static IReadOnlyCollection<SeriesLogoInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            container: $"container-{faker.Random.AlphaNumeric(8)}",
            path: faker.System.FileName("png"),
            sizeInBytes: faker.Random.Long(1, 1_000_000)))];
    }
}
