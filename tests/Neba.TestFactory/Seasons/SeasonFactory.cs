using Bogus;

using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;

namespace Neba.TestFactory.Seasons;

public static class SeasonFactory
{
    public const string ValidDescription = "2025 Season";
    public static readonly DateOnly ValidStartDate = new(2025, 1, 1);
    public static readonly DateOnly ValidEndDate = new(2025, 12, 31);

    public static Season Create(
        SeasonId? id = null,
        string? description = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool complete = false)
    {
        return new Season
        {
            Id = id ?? SeasonId.New(),
            Description = description ?? ValidDescription,
            StartDate = startDate ?? ValidStartDate,
            EndDate = endDate ?? ValidEndDate,
            Complete = complete
        };
    }

    public static IReadOnlyCollection<Season> Bogus(
        int count,
        IReadOnlyCollection<BowlerId>? bowlerIds = null,
        int? seed = null)
    {
        var faker = new Faker<Season>()
            .CustomInstantiator(f => new()
            {
                Id = new SeasonId(Ulid.BogusString(f)),
                Description = $"{f.Date.PastDateOnly(100).Year} Season",
                StartDate = f.Date.PastDateOnly(5),
                EndDate = f.Date.FutureDateOnly(5),
                Complete = f.Random.Bool()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        var seasons = faker.Generate(count);

        if (bowlerIds is { Count: > 0 })
        {
#pragma warning disable CA5394 // Random is acceptable here — used only for test data generation, not security
            var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
#pragma warning restore CA5394
            foreach (var season in seasons.Where(s => s.Complete))
            {
                AssignAwards(season, bowlerIds, rng);
            }
        }

        return seasons;
    }

#pragma warning disable CA5394 // Random is acceptable here — used only for test data generation, not security
    private static void AssignAwards(Season season, IReadOnlyCollection<BowlerId> bowlerIds, Random rng)
    {
        var bowlerList = bowlerIds.ToList();

        // BowlerOfTheYear: 1–3 bowlers assigned random categories with plausible eligibility values
        var categories = BowlerOfTheYearCategory.List.ToArray();
        foreach (var bowlerId in PickRandom(bowlerList, rng))
        {
            var category = categories[rng.Next(categories.Length)];

            if (category == BowlerOfTheYearCategory.Open)
                season.AddOpenBowlerOfTheYearWinner(bowlerId);
            else if (category == BowlerOfTheYearCategory.Woman)
                season.AddWomanOfTheYearWinner(bowlerId, Gender.Female);
            else if (category == BowlerOfTheYearCategory.Senior)
                season.AddSeniorBowlerOfTheYearWinner(bowlerId, age: 55);
            else if (category == BowlerOfTheYearCategory.SuperSenior)
                season.AddSuperSeniorBowlerOfTheYearWinner(bowlerId, age: 65);
            else if (category == BowlerOfTheYearCategory.Rookie)
                season.AddRookieBowlerOfTheYearWinner(bowlerId, isRookie: true);
            else if (category == BowlerOfTheYearCategory.Youth)
                season.AddYouthBowlerOfTheYearWinner(bowlerId, age: 16);
        }

        // HighAverage: 1–3 bowlers sharing the same average
        // statEligibleTournamentCount = 6 → minimum = floor(4.5 × 6) = 27 games
        const int statEligibleTournamentCount = 6;
        const int totalGames = 30; // safely above the 27-game minimum
        var average = (decimal)rng.Next(150, 251);
        foreach (var bowlerId in PickRandom(bowlerList, rng))
        {
            season.AddHighAverageWinner(bowlerId, average, totalGames, tournamentsParticipated: 6, statEligibleTournamentCount);
        }

        // HighBlock: 1–3 bowlers sharing the same score
        // score range 900–1300 keeps well under the 5-game maximum of 1500
        var blockScore = rng.Next(900, 1301);
        foreach (var bowlerId in PickRandom(bowlerList, rng))
        {
            season.AddHighBlockWinner(bowlerId, blockScore, games: 5);
        }
    }

    private static IEnumerable<BowlerId> PickRandom(List<BowlerId> bowlerIds, Random rng)
    {
        var count = rng.Next(1, Math.Min(4, bowlerIds.Count + 1));
        return bowlerIds.OrderBy(_ => rng.Next()).Take(count);
    }
#pragma warning restore CA5394
}