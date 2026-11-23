using Neba.Api.Features.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

internal static class SeriesAdvancementDtoFactory
{
    public const int ValidVersion = 1;
    public const int ValidTargetField = 8;

    /// <summary>Version 1, target field 8, no Backfill date, no Stages and no positions.</summary>
    public static SeriesAdvancementDto Create(
        int? version = null,
        DateOnly? backfillAsOf = null,
        int? targetField = null,
        IReadOnlyList<StageAdvancementDto>? stages = null,
        IReadOnlyList<SeriesPositionDto>? positions = null)
        => new(
            version ?? ValidVersion,
            backfillAsOf,
            targetField ?? ValidTargetField,
            stages ?? [],
            positions ?? []);

    internal static IReadOnlyList<SeriesAdvancementDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            version: faker.Random.Int(0, 50),
            backfillAsOf: new DateOnly(2025, 1, 1).AddDays(faker.Random.Int(0, 364)),
            targetField: faker.Random.Int(1, 40),
            stages: StageAdvancementDtoFactory.Bogus(3, faker),
            positions: SeriesPositionDtoFactory.Bogus(faker.Random.Int(1, 3), faker)))];
    }

    public static IReadOnlyList<SeriesAdvancementDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}