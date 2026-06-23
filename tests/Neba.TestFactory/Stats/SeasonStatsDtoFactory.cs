using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class SeasonStatsDtoFactory
{
    public static SeasonStatsDto Create(
        SeasonWithStatsDto? season = null,
        IReadOnlyCollection<SeasonWithStatsDto>? seasonsWithStats = null,
        IReadOnlyCollection<BowlerSeasonStatsDto>? bowlerStats = null,
        IReadOnlyDictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>? bowlerOfTheYearRaces = null,
        SeasonStatsSummaryDto? summary = null,
        (decimal games, decimal tournaments, decimal entries)? minimums = null)
        => new()
        {
            Season = season ?? SeasonWithStatsDtoFactory.Create(),
            SeasonsWithStats = seasonsWithStats ?? [SeasonWithStatsDtoFactory.Create()],
            BowlerStats = bowlerStats ?? [BowlerSeasonStatsDtoFactory.Create()],
            BowlerOfTheYearRaces = bowlerOfTheYearRaces ?? new Dictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
            {
                [BowlerOfTheYearCategory.Open.Value] = [BowlerOfTheYearPointsRaceSeriesDtoFactory.Create()],
                [BowlerOfTheYearCategory.Senior.Value] = [],
                [BowlerOfTheYearCategory.SuperSenior.Value] = [],
                [BowlerOfTheYearCategory.Woman.Value] = [],
                [BowlerOfTheYearCategory.Youth.Value] = [],
                [BowlerOfTheYearCategory.Rookie.Value] = [],
            },
            Summary = summary ?? SeasonStatsSummaryDtoFactory.Create(),
            MinimumNumberOfGames = minimums?.games ?? 0m,
            MinimumNumberOfTournaments = minimums?.tournaments ?? 0m,
            MinimumNumberOfEntries = minimums?.entries ?? 0m,
        };

    public static IReadOnlyCollection<SeasonStatsDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonStatsDto>()
            .CustomInstantiator(f => new SeasonStatsDto
            {
                Season = SeasonWithStatsDtoFactory.Bogus(1, f).Single(),
                SeasonsWithStats = SeasonWithStatsDtoFactory.Bogus(f.Random.Int(1, 5), f),
                BowlerStats = BowlerSeasonStatsDtoFactory.Bogus(f.Random.Int(1, 10), f),
                BowlerOfTheYearRaces = new Dictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
                {
                    [BowlerOfTheYearCategory.Open.Value] = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(f.Random.Int(1, 5), f),
                    [BowlerOfTheYearCategory.Senior.Value] = [],
                    [BowlerOfTheYearCategory.SuperSenior.Value] = [],
                    [BowlerOfTheYearCategory.Woman.Value] = [],
                    [BowlerOfTheYearCategory.Youth.Value] = [],
                    [BowlerOfTheYearCategory.Rookie.Value] = [],
                },
                Summary = SeasonStatsSummaryDtoFactory.Bogus(1, f).Single(),
                MinimumNumberOfGames = f.Random.Decimal(10, 60),
                MinimumNumberOfTournaments = f.Random.Decimal(2, 8),
                MinimumNumberOfEntries = f.Random.Decimal(3, 12),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<SeasonStatsDto> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}