using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class RecomputeSeriesAdvancementRequestFactory
{
    public static RecomputeSeriesAdvancementRequest Create(
        string? id = null,
        string? stageId = null,
        RecomputeSeriesAdvancementInput? recomputation = null)
        => new()
        {
            Id = id ?? Ulid.NewUlid().ToString(),
            StageId = stageId ?? Ulid.NewUlid().ToString(),
            Recomputation = recomputation ?? RecomputeSeriesAdvancementInputFactory.Create(),
        };

    internal static IReadOnlyCollection<RecomputeSeriesAdvancementRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            id: Ulid.BogusString(faker),
            stageId: Ulid.BogusString(faker),
            recomputation: RecomputeSeriesAdvancementInputFactory.Bogus(1, faker).Single()))];
    }

    public static IReadOnlyCollection<RecomputeSeriesAdvancementRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}