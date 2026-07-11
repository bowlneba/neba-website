using Bogus;

using Neba.Api.Uploads;

namespace Neba.TestFactory.Uploads;

public static class PendingUploadFactory
{
    public const string ValidContainer = "test-container";
    public const string ValidPath = "test-file.txt";
    public static readonly DateTimeOffset ValidUploadedAtUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static PendingUpload Create(
        string? container = null,
        string? path = null,
        DateTimeOffset? uploadedAtUtc = null)
        => new()
        {
            Container = container ?? ValidContainer,
            Path = path ?? ValidPath,
            UploadedAtUtc = uploadedAtUtc ?? ValidUploadedAtUtc
        };

    internal static IReadOnlyCollection<PendingUpload> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new PendingUpload
        {
            Container = $"container-{faker.Random.AlphaNumeric(8)}",
            Path = $"{faker.System.FileName()}.{faker.System.CommonFileExt()}",
            UploadedAtUtc = faker.Date.PastOffset(1)
        })];
    }

    public static IReadOnlyCollection<PendingUpload> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}