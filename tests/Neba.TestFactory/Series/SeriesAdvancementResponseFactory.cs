using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class SeriesAdvancementResponseFactory
{
    public const int ValidVersion = 1;
    public const int ValidTargetField = 8;

    /// <summary>Version 1, target field 8, no Backfill date, one Qualifying Stage and one position.</summary>
    public static SeriesAdvancementResponse Create(
        int? version = null,
        DateOnly? backfillAsOf = null,
        int? targetField = null,
        IReadOnlyList<StageAdvancementResponse>? stages = null,
        IReadOnlyList<SeriesPositionResponse>? positions = null)
        => new()
        {
            Version = version ?? ValidVersion,
            BackfillAsOf = backfillAsOf,
            TargetField = targetField ?? ValidTargetField,
            Stages = stages ?? [StageAdvancementResponseFactory.Create()],
            Positions = positions ?? [SeriesPositionResponseFactory.Create()],
        };

    internal static IReadOnlyList<SeriesAdvancementResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            version: faker.Random.Int(0, 50),
            backfillAsOf: new DateOnly(2025, 1, 1).AddDays(faker.Random.Int(0, 364)),
            targetField: faker.Random.Int(1, 40),
            stages: StageAdvancementResponseFactory.Bogus(3, faker),
            positions: SeriesPositionResponseFactory.Bogus(faker.Random.Int(1, 3), faker)))];
    }

    public static IReadOnlyList<SeriesAdvancementResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}