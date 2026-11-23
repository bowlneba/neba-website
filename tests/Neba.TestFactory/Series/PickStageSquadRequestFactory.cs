using Neba.Api.Contracts.Series.PickStageSquad;

namespace Neba.TestFactory.Series;

public static class PickStageSquadRequestFactory
{
    public static PickStageSquadRequest Create(
        string? id = null,
        string? stageId = null,
        PickStageSquadInput? squad = null)
        => new()
        {
            Id = id ?? Ulid.NewUlid().ToString(),
            StageId = stageId ?? Ulid.NewUlid().ToString(),
            Squad = squad ?? PickStageSquadInputFactory.Create(),
        };

    internal static IReadOnlyCollection<PickStageSquadRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            id: Ulid.BogusString(faker),
            stageId: Ulid.BogusString(faker),
            squad: PickStageSquadInputFactory.Bogus(1, faker).Single()))];
    }

    public static IReadOnlyCollection<PickStageSquadRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}