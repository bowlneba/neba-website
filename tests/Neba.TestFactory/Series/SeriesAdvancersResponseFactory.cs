using Neba.Api.Contracts.Series.GetSeriesAdvancers;

namespace Neba.TestFactory.Series;

public static class SeriesAdvancersResponseFactory
{
    /// <summary>One Official Qualifying Stage with one qualifier.</summary>
    public static SeriesAdvancersResponse Create(IReadOnlyList<StageAdvancersResponse>? stages = null)
        => new()
        {
            Stages = stages ?? [StageAdvancersResponseFactory.Create()],
        };

    internal static IReadOnlyList<SeriesAdvancersResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(StageAdvancersResponseFactory.Bogus(2, faker)))];
    }

    public static IReadOnlyList<SeriesAdvancersResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
