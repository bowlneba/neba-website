using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

internal static class SeriesPositionDtoFactory
{
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPosition = 1;
    public const bool ValidBowled = true;

    /// <summary>Position 1, bowled, with no finalist marker (a Series with no Finale).</summary>
    public static SeriesPositionDto Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? position = null,
        bool? bowled = null,
        bool? isFinalist = null)
        => new(bowlerId ?? BowlerId.New(), bowlerName ?? ValidBowlerName, position ?? ValidPosition, bowled ?? ValidBowled, isFinalist);

    /// <summary>Positions 1..count, each a new bowler.</summary>
    internal static IReadOnlyList<SeriesPositionDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(1, count).Select(position => Create(
            new BowlerId(Ulid.BogusString(faker)),
            faker.Name.FullName(),
            position,
            faker.Random.Bool(),
            faker.Random.Bool() ? faker.Random.Bool() : null))];
    }

    public static IReadOnlyList<SeriesPositionDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}