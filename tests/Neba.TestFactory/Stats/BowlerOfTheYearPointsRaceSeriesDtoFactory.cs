using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class BowlerOfTheYearPointsRaceSeriesDtoFactory
{
    public const string ValidBowlerName = "Test Bowler";

    public static BowlerOfTheYearPointsRaceSeriesDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        IReadOnlyCollection<BowlerOfTheYearPointsRaceTournamentDto>? results = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Results = results ?? [BowlerOfTheYearPointsRaceTournamentDtoFactory.Create()]
        };

    internal static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerOfTheYearPointsRaceSeriesDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            Results = BowlerOfTheYearPointsRaceTournamentDtoFactory.Bogus(faker.Random.Int(1, 5), faker)
        })];
    }

    public static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}