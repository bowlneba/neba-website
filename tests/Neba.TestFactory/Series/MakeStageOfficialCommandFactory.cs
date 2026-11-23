using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.MakeStageOfficial;

namespace Neba.TestFactory.Series;

internal static class MakeStageOfficialCommandFactory
{
    public const int ValidVersion = 3;

    public static MakeStageOfficialCommand Create(
        SeriesId? seriesId = null,
        SeriesStageId? stageId = null,
        int? version = null)
        => new()
        {
            SeriesId = seriesId ?? SeriesId.New(),
            StageId = stageId ?? SeriesStageId.New(),
            Version = version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<MakeStageOfficialCommand> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            new SeriesId(Ulid.BogusString(faker)),
            new SeriesStageId(Ulid.BogusString(faker)),
            faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<MakeStageOfficialCommand> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
