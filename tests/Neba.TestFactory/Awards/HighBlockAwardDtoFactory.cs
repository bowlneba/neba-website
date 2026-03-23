using Neba.Application.Awards;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Awards;

public static class HighBlockAwardDtoFactory
{
    public const string ValidSeason = "2025 Season";
    public const int ValidScore = 1350;

    public static HighBlockAwardDto Create(
        string? season = null,
        Name? bowlerName = null,
        int? score = null)
        => new()
        {
            Season = season ?? ValidSeason,
            BowlerName = bowlerName ?? NameFactory.Create(),
            Score = score ?? ValidScore
        };

    public static IReadOnlyCollection<HighBlockAwardDto> Bogus(int count, int? seed = null)
    {
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Bogus.Faker<HighBlockAwardDto>()
            .CustomInstantiator(f => new()
            {
                Season = $"Season {f.Date.Past(5).Year}",
                BowlerName = bowlerNames.GetNext(),
                Score = f.Random.Int(1250, 1400)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}