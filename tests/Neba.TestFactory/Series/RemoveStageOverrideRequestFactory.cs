using Neba.Api.Features.Series.RemoveStageOverride;

namespace Neba.TestFactory.Series;

internal static class RemoveStageOverrideRequestFactory
{
    public const int ValidVersion = 3;

    /// <summary>Pass <paramref name="omitVersion"/> to build a request with no version, as when the query value is missing.</summary>
    public static RemoveStageOverrideRequest Create(
        string? id = null,
        string? stageId = null,
        string? overrideId = null,
        int? version = null,
        bool omitVersion = false)
        => new()
        {
            Id = id ?? Ulid.NewUlid().ToString(),
            StageId = stageId ?? Ulid.NewUlid().ToString(),
            OverrideId = overrideId ?? Ulid.NewUlid().ToString(),
            Version = omitVersion ? null : version ?? ValidVersion,
        };

    internal static IReadOnlyCollection<RemoveStageOverrideRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            id: Ulid.BogusString(faker),
            stageId: Ulid.BogusString(faker),
            overrideId: Ulid.BogusString(faker),
            version: faker.Random.Int(0, 50)))];
    }

    public static IReadOnlyCollection<RemoveStageOverrideRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}