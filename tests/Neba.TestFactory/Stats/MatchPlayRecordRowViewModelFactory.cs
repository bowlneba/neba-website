using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayRecordRowViewModelFactory
{
    public static MatchPlayRecordRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? wins = null,
        int? loses = null,
        int? finals = null,
        decimal? matchPlayAverage = null,
        decimal? winnings = null)
        => new()
        {
            Rank = rank ?? 1,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? "Test Bowler",
            Wins = wins ?? 5,
            Loses = loses ?? 5,
            Finals = finals ?? 2,
            MatchPlayAverage = matchPlayAverage ?? 200m,
            Winnings = winnings ?? 1000m
        };

    public static IReadOnlyCollection<MatchPlayRecordRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        var totalGames = Math.Max(count + 5, 10);

        var faker = new Faker<MatchPlayRecordRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var loses = currentRank - 1;
                var wins = totalGames - loses;

                return new MatchPlayRecordRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    Wins = wins,
                    Loses = loses,
                    Finals = f.Random.Int(0, 10),
                    MatchPlayAverage = f.Random.Decimal(150, 250),
                    Winnings = f.Random.Decimal(0, 10000)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}