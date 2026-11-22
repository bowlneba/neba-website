using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.RecomputeSeriesAdvancement;

namespace Neba.TestFactory.Series;

internal static class RecomputeSeriesAdvancementCommandFactory
{
    public const int ValidVersion = 3;
    public const bool ValidConfirmResetOfficial = false;

    public static RecomputeSeriesAdvancementCommand Create(
        SeriesId? seriesId = null,
        SeriesStageId? stageId = null,
        int? version = null,
        bool? confirmResetOfficial = null)
        => new()
        {
            SeriesId = seriesId ?? SeriesId.New(),
            StageId = stageId ?? SeriesStageId.New(),
            Version = version ?? ValidVersion,
            ConfirmResetOfficial = confirmResetOfficial ?? ValidConfirmResetOfficial,
        };

    internal static IReadOnlyCollection<RecomputeSeriesAdvancementCommand> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            seriesId: new SeriesId(Ulid.BogusString(faker)),
            stageId: new SeriesStageId(Ulid.BogusString(faker)),
            version: faker.Random.Int(0, 50),
            confirmResetOfficial: faker.Random.Bool()))];
    }

    public static IReadOnlyCollection<RecomputeSeriesAdvancementCommand> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}