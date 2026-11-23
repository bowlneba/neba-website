using Neba.Api.Contracts.Series.CreateStageOverride;

namespace Neba.TestFactory.Series;

public static class CreatedStageOverrideResponseFactory
{
    public const string ValidOverrideId = "01JBQ5Y7K3M9N2P4R6T8V0W3B1";
    public const int ValidVersion = 4;

    public static CreatedStageOverrideResponse Create(
        string? overrideId = null,
        int? version = null)
        => new()
        {
            OverrideId = overrideId ?? ValidOverrideId,
            Version = version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<CreatedStageOverrideResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            overrideId: Ulid.BogusString(faker),
            version: faker.Random.Int(1, 50)))];
    }

    public static IReadOnlyCollection<CreatedStageOverrideResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}