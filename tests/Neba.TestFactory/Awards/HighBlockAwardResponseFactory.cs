using Bogus;

using Neba.Api.Contracts.Awards;

namespace Neba.TestFactory.Awards;

public static class HighBlockAwardResponseFactory
{
    public const string ValidSeason = "2025 Season";
    public const string ValidBowlerName = "Joe Bowler";
    public const int ValidSocre = 1300;

    public static HighBlockAwardResponse Create(
        string? season = null,
        string? bowlerName = null,
        int? score = null)
        => new()
        {
            Season = season ?? ValidSeason,
            BowlerName = bowlerName ?? ValidBowlerName,
            Score = score ?? ValidSocre
        };

    public static IReadOnlyCollection<HighBlockAwardResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HighBlockAwardResponse>()
            .CustomInstantiator(f => new()
            {
                Season = $"{f.Date.PastDateOnly(100).Year} Season",
                BowlerName = f.Person.FullName,
                Score = f.Random.Int(1250,1400)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}