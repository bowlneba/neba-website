using Bogus;

using Neba.Website.Server.History.Awards;

namespace Neba.TestFactory.Awards;

public static class HighBlockAwardViewModelFactory
{
    public const string ValidSeason = "2025 Season";
    public const int ValidScore = 1300;
    public static readonly IReadOnlyCollection<string> ValidBowlers = ["Joe Bowler"];

    public static HighBlockAwardViewModel Create(
        string? season = null,
        IReadOnlyCollection<string>? bowlers = null,
        int? score = null)
        => new()
        {
            Season = season ?? ValidSeason,
            Bowlers = bowlers ?? ValidBowlers,
            Score = score ?? ValidScore
        };

    public static IReadOnlyCollection<HighBlockAwardViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HighBlockAwardViewModel>()
            .CustomInstantiator(f => new()
            {
                Season = $"{f.Date.PastDateOnly(100).Year} Season",
                Bowlers = [f.Person.FullName],
                Score = f.Random.Int(1250, 1400)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
