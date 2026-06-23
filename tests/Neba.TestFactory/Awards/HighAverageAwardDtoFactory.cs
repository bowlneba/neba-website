using Neba.Api.Features.Awards.ListHighAverageAwards;
using Neba.Api.Features.Bowlers.Domain;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Awards;

public static class HighAverageAwardDtoFactory
{
    public const string ValidSeason = "2025 Season";
    public const decimal ValidAverage = 230.55m;
    public const int ValidTotalGames = 45;
    public const int ValidTournamentsParticipated = 10;

    public static HighAverageAwardDto Create(
        string? season = null,
        Name? bowlerName = null,
        decimal? average = null,
        int? totalGames = null,
        int? tournamentsParticipated = null)
        => new()
        {
            Season = season ?? ValidSeason,
            BowlerName = bowlerName ?? NameFactory.Create(),
            Average = average ?? ValidAverage,
            TotalGames = totalGames ?? ValidTotalGames,
            TournamentsParticipated = tournamentsParticipated ?? ValidTournamentsParticipated
        };

    public static IReadOnlyCollection<HighAverageAwardDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new HighAverageAwardDto
        {
            Season = faker.Random.Words(2),
            BowlerName = bowlerNames.GetNext(),
            Average = faker.Random.Decimal(225, 235),
            TotalGames = faker.Random.Int(70, 100),
            TournamentsParticipated = faker.Random.Int(10, 20)
        })];
    }

    public static IReadOnlyCollection<HighAverageAwardDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}