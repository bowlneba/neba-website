using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class SeriesPositionResponseFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPosition = 1;
    public const bool ValidBowled = true;

    /// <summary>Position 1, bowled, with no finalist marker (a Series with no Finale).</summary>
    public static SeriesPositionResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? position = null,
        bool? bowled = null,
        bool? isFinalist = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Position = position ?? ValidPosition,
            Bowled = bowled ?? ValidBowled,
            IsFinalist = isFinalist,
        };

    /// <summary>Positions 1..count, each a new bowler.</summary>
    internal static IReadOnlyList<SeriesPositionResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(1, count).Select(position => Create(
            Ulid.BogusString(faker),
            faker.Name.FullName(),
            position,
            faker.Random.Bool(),
            faker.Random.Bool() ? faker.Random.Bool() : null))];
    }

    public static IReadOnlyList<SeriesPositionResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}