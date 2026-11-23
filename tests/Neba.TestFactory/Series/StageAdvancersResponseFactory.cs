using Neba.Api.Contracts.Series.GetSeriesAdvancers;

namespace Neba.TestFactory.Series;

public static class StageAdvancersResponseFactory
{
    public const string ValidStageId = "01JBQ5Y7K3M9N2P4R6T8V0W3B1";
    public const int ValidOrder = 1;
    public const string ValidKind = "Qualifying";

    /// <summary>The first Qualifying Stage with one qualifier.</summary>
    public static StageAdvancersResponse Create(
        string? stageId = null,
        int? order = null,
        string? kind = null,
        IReadOnlyList<AdvancerResponse>? advancers = null)
        => new()
        {
            StageId = stageId ?? ValidStageId,
            Order = order ?? ValidOrder,
            Kind = kind ?? ValidKind,
            Advancers = advancers ?? [AdvancerResponseFactory.Create()],
        };

    internal static IReadOnlyList<StageAdvancersResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(index => Create(
            Ulid.BogusString(faker),
            index + 1,
            "Qualifying",
            AdvancerResponseFactory.Bogus(faker.Random.Int(1, 3), faker)))];
    }

    public static IReadOnlyList<StageAdvancersResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
