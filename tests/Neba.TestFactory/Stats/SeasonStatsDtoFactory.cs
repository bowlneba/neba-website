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

    internal static IReadOnlyCollection<SeasonStatsDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SeasonStatsDto
        {
            Season = SeasonWithStatsDtoFactory.Bogus(1, faker).Single(),
            SeasonsWithStats = SeasonWithStatsDtoFactory.Bogus(faker.Random.Int(1, 5), faker),
            BowlerStats = BowlerSeasonStatsDtoFactory.Bogus(faker.Random.Int(1, 10), faker),
            BowlerOfTheYearRaces = new Dictionary<int, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
            {
                [BowlerOfTheYearCategory.Open.Value] = BowlerOfTheYearPointsRaceSeriesDtoFactory.Bogus(faker.Random.Int(1, 5), faker),
                [BowlerOfTheYearCategory.Senior.Value] = [],
                [BowlerOfTheYearCategory.SuperSenior.Value] = [],
                [BowlerOfTheYearCategory.Woman.Value] = [],
                [BowlerOfTheYearCategory.Youth.Value] = [],
                [BowlerOfTheYearCategory.Rookie.Value] = [],
            },
            Summary = SeasonStatsSummaryDtoFactory.Bogus(1, faker).Single(),
            MinimumNumberOfGames = faker.Random.Decimal(10, 60),
            MinimumNumberOfTournaments = faker.Random.Decimal(2, 8),
            MinimumNumberOfEntries = faker.Random.Decimal(3, 12),
        })];
    }

    public static IReadOnlyCollection<SeasonStatsDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}