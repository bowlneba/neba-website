using Neba.Website.Server.Tournaments.Schedule;

namespace Neba.TestFactory.Series;

public static class SeriesFinaleItemViewModelFactory
{
    public const string ValidSeriesId = "01JBQ5Y7K3M9N2P4R6T8V0W1X4";
    public const string ValidSeriesName = "Vincent Hall Memorial Series";
    public const string ValidSeasonId = SeriesInputFactory.ValidSeasonId;
    public const int ValidFinalistCount = 4;
    public static readonly DateOnly ValidDate = new(2025, 9, 27);
    public static readonly TimeOnly ValidStartTime = new(14, 0);

    /// <summary>Four finalists on 9/27/2025 at 2:00 PM, no logo, no host center yet.</summary>
    public static SeriesFinaleItemViewModel Create(
        string? seriesId = null,
        string? seriesName = null,
        string? seasonId = null,
        Uri? logoUrl = null,
        int? finalistCount = null,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        string? host = null)
        => new()
        {
            SeriesId = seriesId ?? ValidSeriesId,
            SeriesName = seriesName ?? ValidSeriesName,
            SeasonId = seasonId ?? ValidSeasonId,
            LogoUrl = logoUrl,
            FinalistCount = finalistCount ?? ValidFinalistCount,
            Date = date ?? ValidDate,
            StartTime = startTime ?? ValidStartTime,
            Host = host,
        };

    public static IReadOnlyCollection<SeriesFinaleItemViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            seriesId: Ulid.BogusString(faker),
            seriesName: $"{faker.Company.CompanyName()} Series",
            seasonId: Ulid.BogusString(faker),
            finalistCount: faker.Random.Int(2, 12),
            date: DateOnly.FromDateTime(faker.Date.Soon(120)),
            startTime: new TimeOnly(faker.Random.Int(9, 18), 0)))];
    }
}