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
        SeasonStatsSummaryDto? summary = null,
        (decimal games, decimal tournaments, decimal entries)? minimums = null)
        => new()
        {
            Season = season ?? SeasonDtoFactory.Create(),
            SeasonsWithStats = seasonsWithStats ?? [SeasonDtoFactory.Create()],
            BowlerStats = bowlerStats ?? [BowlerSeasonStatsDtoFactory.Create()],
            BowlerOfTheYearRace = bowlerOfTheYearRace ?? [BowlerOfTheYearPointsRaceSeriesDtoFactory.Create()],
            Summary = summary ?? SeasonStatsSummaryDtoFactory.Create(),
            MinimumNumberOfGames = minimums?.games ?? 0m,
            MinimumNumberOfTournaments = minimums?.tournaments ?? 0m,
            MinimumNumberOfEntries = minimums?.entries ?? 0m,
        };

    public static IReadOnlyCollection<SeasonStatsDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonStatsDto>()
            .CustomInstantiator(f =>
            {
                var seasonSeed = f.Random.Int(1, int.MaxValue);
                var seasonsWithStatsSeed = f.Random.Int(1, int.MaxValue);
                var bowlerStatsSeed = f.Random.Int(1, int.MaxValue);
                var bowlerOfTheYearRaceSeed = f.Random.Int(1, int.MaxValue);
                var summarySeed = f.Random.Int(1, int.MaxValue);

                return new SeasonStatsDto
                {
                    Season = SeasonDtoFactory.Bogus(1, seasonSeed).Single(),
                    SeasonsWithStats = SeasonDtoFactory.Bogus(f.Random.Int(1, 5), seasonsWithStatsSeed),
                    BowlerStats = BowlerSeasonStatsDtoFactory.Bogus(f.Random.Int(1, 10), bowlerStatsSeed),
                    BowlerOfTheYearRace = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(f.Random.Int(1, 5), bowlerOfTheYearRaceSeed),
                    Summary = SeasonStatsSummaryDtoFactory.Bogus(1, summarySeed).Single(),
                    MinimumNumberOfGames = f.Random.Decimal(10, 60),
                    MinimumNumberOfTournaments = f.Random.Decimal(2, 8),
                    MinimumNumberOfEntries = f.Random.Decimal(3, 12),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
