using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.ListStageResults;

namespace Neba.TestFactory.Series;

internal static class StageResultSummaryDtoFactory
{
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPlace = 1;

    public static StageResultSummaryDto Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? place = null)
        => new(bowlerId ?? BowlerId.New(), bowlerName ?? ValidBowlerName, place ?? ValidPlace);

    /// <summary>Rows placed 1..count, each a new bowler.</summary>
    internal static IReadOnlyList<StageResultSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(1, count).Select(place => Create(
            new BowlerId(Ulid.BogusString(faker)),
            faker.Name.FullName(),
            place))];
    }

    public static IReadOnlyList<StageResultSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}