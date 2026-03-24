using Neba.Application.Awards.ListHighAverageAwards;
using Neba.Domain.Bowlers;
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

    public static IReadOnlyCollection<HighAverageAwardDto> Bogus(int count, int? seed = null)
    {
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Bogus.Faker<HighAverageAwardDto>()
            .CustomInstantiator(f => new()
            {
                Season = f.Random.Words(2),
                BowlerName = bowlerNames.GetNext(),
                Average = f.Random.Decimal(225, 235),
                TotalGames = f.Random.Int(70, 100),
                TournamentsParticipated = f.Random.Int(10, 20)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}