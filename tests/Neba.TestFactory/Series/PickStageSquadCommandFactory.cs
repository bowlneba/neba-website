using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.PickStageSquad;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

internal static class PickStageSquadCommandFactory
{
    public const int ValidVersion = 3;

    public static PickStageSquadCommand Create(
        SeriesId? seriesId = null,
        SeriesStageId? stageId = null,
        SquadId? squadId = null,
        int? version = null)
        => new()
        {
            SeriesId = seriesId ?? SeriesId.New(),
            StageId = stageId ?? SeriesStageId.New(),
            SquadId = squadId ?? SquadId.New(),
            Version = version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<PickStageSquadCommand> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            seriesId: new SeriesId(Ulid.BogusString(faker)),
            stageId: new SeriesStageId(Ulid.BogusString(faker)),
            squadId: new SquadId(Ulid.BogusString(faker)),
            version: faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<PickStageSquadCommand> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}