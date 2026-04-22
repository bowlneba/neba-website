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

    public static IReadOnlyCollection<PointsRaceSeriesResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<PointsRaceSeriesResponse>()
            .CustomInstantiator(f => new PointsRaceSeriesResponse
            {
                BowlerId = Ulid.BogusString(f),
                BowlerName = f.Name.FullName(),
                Results = PointsRaceTournamentResponseFactory.Bogus(f.Random.Int(1, 10), seed)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}