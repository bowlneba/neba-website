using Neba.Api.Contracts.Series.ListStageResults;

namespace Neba.TestFactory.Series;

public static class StageResultSummaryResponseFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPlace = 1;

    public static StageResultSummaryResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? place = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Place = place ?? ValidPlace,
        };

    /// <summary>Rows placed 1..count, each a new bowler.</summary>
    internal static IReadOnlyList<StageResultSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(1, count).Select(place => Create(
            Ulid.BogusString(faker),
            faker.Name.FullName(),
            place))];
    }

    public static IReadOnlyList<StageResultSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}