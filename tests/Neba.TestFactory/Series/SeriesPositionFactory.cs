using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class SeriesPositionFactory
{
    public const int ValidPosition = 1;

    /// <summary>Position 1, bowled, in a Series with no Finale.</summary>
    public static SeriesPosition Create(
        BowlerId? bowlerId = null,
        int? position = null,
        bool bowled = true,
        bool? isFinalist = null)
    {
        var result = SeriesPosition.Create(bowlerId ?? BowlerId.New(), position ?? ValidPosition, bowled, isFinalist);

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create series position: {result.Errors[0].Description}")
            : result.Value;
    }

    /// <summary>Positions 1..count, each a new bowler who bowled.</summary>
    public static IReadOnlyCollection<SeriesPosition> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(1, count).Select(position => Create(
            new BowlerId(Ulid.BogusString(faker)),
            position))];
    }
}