using Neba.Api.Features.Awards.ListHighBlockAwards;
using Neba.Api.Features.Bowlers.Domain;
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

    public static IReadOnlyCollection<HighBlockAwardDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNames = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new HighBlockAwardDto
        {
            Season = $"Season {faker.Date.Past(5).Year}",
            BowlerName = bowlerNames.GetNext(),
            Score = faker.Random.Int(1250, 1400)
        })];
    }

    public static IReadOnlyCollection<HighBlockAwardDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}