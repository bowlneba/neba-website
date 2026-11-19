using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SeriesFinaleViewModelFactory
{
    public const int ValidFinalistCount = 4;
    public static readonly DateOnly ValidDate = new(2025, 9, 27);
    public static readonly TimeOnly ValidStartTime = new(14, 0);

    /// <summary>Four finalists on 9/27/2025 at 2:00 PM with no host center yet.</summary>
    internal static SeriesFinaleViewModel Create(
        int? finalistCount = null,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        string? bowlingCenterId = null,
        string? host = null)
        => new()
        {
            FinalistCount = finalistCount ?? ValidFinalistCount,
            Date = date ?? ValidDate,
            StartTime = startTime ?? ValidStartTime,
            BowlingCenterId = bowlingCenterId,
            Host = host,
        };

    internal static IReadOnlyCollection<SeriesFinaleViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            finalistCount: faker.Random.Int(2, 20),
            date: faker.Date.FutureDateOnly(1),
            startTime: new TimeOnly(faker.Random.Int(8, 18), 0),
            bowlingCenterId: faker.Random.Int(1000, 9999).ToString(System.Globalization.CultureInfo.InvariantCulture),
            host: faker.Company.CompanyName()))];
    }
}