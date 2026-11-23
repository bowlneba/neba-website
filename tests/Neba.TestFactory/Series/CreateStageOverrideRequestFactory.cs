using Neba.Api.Contracts.Series.CreateStageOverride;

namespace Neba.TestFactory.Series;

public static class CreateStageOverrideRequestFactory
{
    public static CreateStageOverrideRequest Create(
        string? id = null,
        string? stageId = null,
        CreateStageOverrideInput? stageOverride = null)
        => new()
        {
            Id = id ?? Ulid.NewUlid().ToString(),
            StageId = stageId ?? Ulid.NewUlid().ToString(),
            StageOverride = stageOverride ?? CreateStageOverrideInputFactory.Create(),
        };

    internal static IReadOnlyCollection<CreateStageOverrideRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            id: Ulid.BogusString(faker),
            stageId: Ulid.BogusString(faker),
            stageOverride: CreateStageOverrideInputFactory.Bogus(1, faker).Single()))];
    }

    public static IReadOnlyCollection<CreateStageOverrideRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}