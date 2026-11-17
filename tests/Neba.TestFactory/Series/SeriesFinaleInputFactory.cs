using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.TestFactory.Series;

public static class SeriesFinaleInputFactory
{
    public const int ValidFinalistCount = 4;
    public static readonly DateOnly ValidDate = new(2025, 9, 27);
    public static readonly TimeOnly ValidStartTime = new(14, 0);

    public static SeriesFinaleInput Create(
        int? finalistCount = null,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        string? bowlingCenterId = null)
        => new()
        {
            FinalistCount = finalistCount ?? ValidFinalistCount,
            Date = date ?? ValidDate,
            StartTime = startTime ?? ValidStartTime,
            BowlingCenterId = bowlingCenterId,
        };

    public static IReadOnlyCollection<SeriesFinaleInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            finalistCount: faker.Random.Int(1, 10),
            date: faker.Date.FutureDateOnly(1),
            startTime: new TimeOnly(faker.Random.Int(8, 20), 0)))];
    }
}
