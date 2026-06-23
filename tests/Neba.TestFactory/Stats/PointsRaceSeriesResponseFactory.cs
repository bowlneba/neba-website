using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class PointsRaceSeriesResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000013";
    public const string ValidBowlerName = "Jane Smith";

    public static PointsRaceSeriesResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        IReadOnlyCollection<PointsRaceTournamentResponse>? results = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Results = results ?? [PointsRaceTournamentResponseFactory.Create()]
        };

    public static IReadOnlyCollection<PointsRaceSeriesResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new PointsRaceSeriesResponse
        {
            BowlerId = Ulid.BogusString(faker),
            BowlerName = faker.Name.FullName(),
            Results = PointsRaceTournamentResponseFactory.Bogus(faker.Random.Int(1, 10), faker)
        })];
    }

    public static IReadOnlyCollection<PointsRaceSeriesResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}