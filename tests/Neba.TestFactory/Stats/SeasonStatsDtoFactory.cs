using Bogus;

using Neba.Application.Seasons;
using Neba.Application.Stats.GetSeasonStats;
using Neba.TestFactory.Seasons;

namespace Neba.TestFactory.Stats;

public static class SeasonStatsDtoFactory
{
    public static SeasonStatsDto Create(
        SeasonDto? season = null,
        IReadOnlyCollection<SeasonDto>? seasonsWithStats = null,
        IReadOnlyCollection<BowlerSeasonStatsDto>? bowlerStats = null,
        IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>? bowlerOfTheYearRace = null,
        SeasonStatsSummaryDto? summary = null)
        => new()
        {
            Season = season ?? SeasonDtoFactory.Create(),
            SeasonsWithStats = seasonsWithStats ?? [SeasonDtoFactory.Create()],
            BowlerStats = bowlerStats ?? [BowlerSeasonStatsDtoFactory.Create()],
            BowlerOfTheYearRace = bowlerOfTheYearRace ?? [BowlerOfTheYearPointsRaceSeriesDtoFactory.Create()],
            Summary = summary ?? SeasonStatsSummaryDtoFactory.Create(),
        };

    public static IReadOnlyCollection<SeasonStatsDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonStatsDto>()
            .CustomInstantiator(f => new SeasonStatsDto
            {
                Season = SeasonDtoFactory.Bogus(1, seed).Single(),
                SeasonsWithStats = SeasonDtoFactory.Bogus(f.Random.Int(1, 5), seed),
                BowlerStats = BowlerSeasonStatsDtoFactory.Bogus(f.Random.Int(1, 10), seed),
                BowlerOfTheYearRace = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(f.Random.Int(1, 5), seed),
                Summary = SeasonStatsSummaryDtoFactory.Bogus(1, seed).Single(),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
