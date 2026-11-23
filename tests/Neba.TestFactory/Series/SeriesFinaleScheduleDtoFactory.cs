using Neba.Api.Features.Series.ListSeriesFinales;

namespace Neba.TestFactory.Series;

internal static class SeriesFinaleScheduleDtoFactory
{
    /// <summary>Four finalists on 9/27/2025 at 2:00 PM with no host center yet.</summary>
    public static SeriesFinaleScheduleDto Create(
        int? finalistCount = null,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        string? bowlingCenterId = null,
        string? bowlingCenterName = null,
        string? bowlingCenterCity = null,
        string? bowlingCenterState = null)
        => new()
        {
            FinalistCount = finalistCount ?? SeriesFinaleDefinitionFactory.ValidFinalistCount,
            Date = date ?? SeriesFinaleDefinitionFactory.ValidDate,
            StartTime = startTime ?? SeriesFinaleDefinitionFactory.ValidStartTime,
            BowlingCenterId = bowlingCenterId,
            BowlingCenterName = bowlingCenterName,
            BowlingCenterCity = bowlingCenterCity,
            BowlingCenterState = bowlingCenterState,
        };

    public static IReadOnlyCollection<SeriesFinaleScheduleDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            finalistCount: faker.Random.Int(1, 10),
            date: faker.Date.FutureDateOnly(1),
            startTime: new TimeOnly(faker.Random.Int(8, 20), 0),
            bowlingCenterId: faker.Random.Number(10000, 99999).ToString(System.Globalization.CultureInfo.InvariantCulture),
            bowlingCenterName: faker.Company.CompanyName(),
            bowlingCenterCity: faker.Address.City(),
            bowlingCenterState: faker.Address.StateAbbr()))];
    }
}