using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class AlreadyAdvancedResponseFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W3A2";
    public const string ValidBowlerName = "Kyle Wilks";
    public const int ValidPlace = 2;
    public const string ValidEarlierStageId = "01JBQ5Y7K3M9N2P4R6T8V0W1X4";
    public const int ValidEarlierStageOrder = 1;

    /// <summary>A bowler who placed second in a later tournament but had already advanced from the first.</summary>
    public static AlreadyAdvancedResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? place = null,
        string? earlierStageId = null,
        int? earlierStageOrder = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Place = place ?? ValidPlace,
            EarlierStageId = earlierStageId ?? ValidEarlierStageId,
            EarlierStageOrder = earlierStageOrder ?? ValidEarlierStageOrder,
        };

    internal static IReadOnlyList<AlreadyAdvancedResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(index => Create(
            Ulid.BogusString(faker),
            faker.Name.FullName(),
            index + 1,
            Ulid.BogusString(faker),
            faker.Random.Int(1, 5)))];
    }

    public static IReadOnlyList<AlreadyAdvancedResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}