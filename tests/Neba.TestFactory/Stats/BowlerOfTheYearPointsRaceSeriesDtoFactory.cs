using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
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

    public static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<BowlerOfTheYearPointsRaceSeriesDto>()
            .CustomInstantiator(f => new BowlerOfTheYearPointsRaceSeriesDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                Results = BowlerOfTheYearPointsRaceTournamentDtoFactory.Bogus(f.Random.Int(1, 5), f.Random.Int(1, int.MaxValue))
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}