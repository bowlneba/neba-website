using Neba.Api.Contracts.Series.PickStageSquad;

namespace Neba.TestFactory.Series;

public static class PickStageSquadInputFactory
{
    public const int ValidVersion = 3;

    public static PickStageSquadInput Create(
        string? squadId = null,
        int? version = null)
        => new()
        {
            SquadId = squadId ?? Ulid.NewUlid().ToString(),
            Version = version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<PickStageSquadInput> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            squadId: Ulid.BogusString(faker),
            version: faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<PickStageSquadInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}