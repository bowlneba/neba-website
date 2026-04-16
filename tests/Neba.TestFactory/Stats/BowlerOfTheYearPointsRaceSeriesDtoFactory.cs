using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;

namespace Neba.TestFactory.Stats;

public static class BowlerOfTheYearPointsRaceSeriesDtoFactory
{
    public const string ValidBowlerName = "Test Bowler";

    public static BowlerOfTheYearPointsRaceSeriesDto Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        IReadOnlyCollection<BowlerOfTheYearPointsRaceTournamentDto>? results = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Results = results ?? [BowlerOfTheYearPointsRaceTournamentDtoFactory.Create()]
        };

    public static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerOfTheYearPointsRaceSeriesDto>()
            .CustomInstantiator(f => new BowlerOfTheYearPointsRaceSeriesDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = f.Name.FullName(),
                Results = BowlerOfTheYearPointsRaceTournamentDtoFactory.Bogus(f.Random.Int(1, 5), seed)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
