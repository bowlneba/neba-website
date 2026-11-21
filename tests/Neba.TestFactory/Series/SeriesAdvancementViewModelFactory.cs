using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class SeriesAdvancementViewModelFactory
{
    public const int ValidVersion = 0;
    public const int ValidTargetField = 8;

    /// <summary>Version 0 with one Pending Qualifying Stage, no positions and no Backfill date.</summary>
    internal static SeriesAdvancementViewModel Create(
        int? version = null,
        DateOnly? backfillAsOf = null,
        int? targetField = null,
        IReadOnlyList<StageAdvancementViewModel>? stages = null,
        IReadOnlyList<PositionViewModel>? positions = null)
        => new()
        {
            Version = version ?? ValidVersion,
            BackfillAsOf = backfillAsOf,
            TargetField = targetField ?? ValidTargetField,
            Stages = stages ?? [StageAdvancementViewModelFactory.Create()],
            Positions = positions ?? [],
        };

    internal static IReadOnlyCollection<SeriesAdvancementViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            version: faker.Random.Int(0, 50),
            backfillAsOf: faker.Date.PastDateOnly(1),
            targetField: faker.Random.Int(1, 64),
            stages: StageAdvancementViewModelFactory.Bogus(faker.Random.Int(1, 6), faker.Random.Int()),
            positions: PositionViewModelFactory.Bogus(faker.Random.Int(0, 6), faker.Random.Int())))];
    }
}