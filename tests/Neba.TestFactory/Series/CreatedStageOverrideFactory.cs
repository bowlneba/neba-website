using Neba.Api.Features.Series.CreateStageOverride;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

internal static class CreatedStageOverrideFactory
{
    public const int ValidVersion = 4;

    public static CreatedStageOverride Create(
        StageOverrideId? overrideId = null,
        int? version = null)
        => new(overrideId ?? StageOverrideId.New(), version ?? ValidVersion);

    internal static IReadOnlyCollection<CreatedStageOverride> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            overrideId: new StageOverrideId(Ulid.BogusString(faker)),
            version: faker.Random.Int(1, 50)))];
    }

    public static IReadOnlyCollection<CreatedStageOverride> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}