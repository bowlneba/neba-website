using Neba.Api.Contracts.Awards;

namespace Neba.TestFactory.Awards;

public static class HighBlockAwardResponseFactory
{
    public const string ValidSeason = "2025 Season";
    public const string ValidBowlerName = "Joe Bowler";
    public const int ValidScore = 1300;

    public static HighBlockAwardResponse Create(
        string? season = null,
        string? bowlerName = null,
        int? score = null)
        => new()
        {
            Season = season ?? ValidSeason,
            BowlerName = bowlerName ?? ValidBowlerName,
            Score = score ?? ValidScore
        };

    public static IReadOnlyCollection<HighBlockAwardResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new HighBlockAwardResponse
        {
            Season = $"{faker.Date.PastDateOnly(100).Year} Season",
            BowlerName = faker.Person.FullName,
            Score = faker.Random.Int(1250, 1400)
        })];
    }

    public static IReadOnlyCollection<HighBlockAwardResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}