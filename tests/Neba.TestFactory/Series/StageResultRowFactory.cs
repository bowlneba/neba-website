using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class StageResultRowFactory
{
    public const int ValidPlace = 1;

    public static StageResultRow Create(BowlerId? bowlerId = null, int? place = null)
        => new(bowlerId ?? BowlerId.New(), place ?? ValidPlace);

    /// <summary>Rows placed 1..count, each a new bowler.</summary>
    public static IReadOnlyList<StageResultRow> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(1, count)
            .Select(place => Create(new BowlerId(Ulid.BogusString(faker)), place))];
    }
}