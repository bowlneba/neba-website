using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.RemoveStageOverride;

namespace Neba.TestFactory.Series;

internal static class RemoveStageOverrideCommandFactory
{
    public const int ValidVersion = 3;

    public static RemoveStageOverrideCommand Create(
        SeriesId? seriesId = null,
        SeriesStageId? stageId = null,
        StageOverrideId? overrideId = null,
        int? version = null)
        => new()
        {
            SeriesId = seriesId ?? SeriesId.New(),
            StageId = stageId ?? SeriesStageId.New(),
            OverrideId = overrideId ?? StageOverrideId.New(),
            Version = version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<RemoveStageOverrideCommand> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            seriesId: new SeriesId(Ulid.BogusString(faker)),
            stageId: new SeriesStageId(Ulid.BogusString(faker)),
            overrideId: new StageOverrideId(Ulid.BogusString(faker)),
            version: faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<RemoveStageOverrideCommand> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}