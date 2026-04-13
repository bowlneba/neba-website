using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class FullStatModalRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidPoints = 100;
    public const decimal ValidAverage = 200m;
    public const int ValidGames = 20;
    public const int ValidFinals = 3;
    public const int ValidWins = 5;
    public const int ValidLoses = 5;
    public const decimal ValidWinnings = 1000m;
    public const decimal ValidFieldAverage = 200m;
    public const int ValidTouranments = 10;

    public static FullStatModalRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        decimal? average = null,
        int? games = null,
        int? finals = null,
        int? wins = null,
        int? loses = null,
        decimal? matchPlayAverage = null,
        decimal? winnings = null,
        decimal? fieldAverage = null,
        int? touranments = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Points = points ?? ValidPoints,
            Average = average ?? ValidAverage,
            Games = games ?? ValidGames,
            Finals = finals ?? ValidFinals,
            Wins = wins ?? ValidWins,
            Loses = loses ?? ValidLoses,
            MatchPlayAverage = matchPlayAverage,
            Winnings = winnings ?? ValidWinnings,
            FieldAverage = fieldAverage ?? ValidFieldAverage,
            Touranments = touranments ?? ValidTouranments
        };

    public static IReadOnlyCollection<FullStatModalRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        var totalMatchPlayGames = Math.Max(count + 5, 10);

        var faker = new Faker<FullStatModalRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var loses = currentRank - 1;
                var wins = totalMatchPlayGames - loses;

                return new FullStatModalRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    Points = Math.Max(0, count - currentRank + 1) * 10,
                    Average = f.Random.Decimal(150, 250),
                    Games = f.Random.Int(10, 60),
                    Finals = f.Random.Int(0, 10),
                    Wins = wins,
                    Loses = loses,
                    MatchPlayAverage = f.Random.Bool() ? f.Random.Decimal(150, 250) : null,
                    Winnings = f.Random.Decimal(0, 10000),
                    FieldAverage = f.Random.Decimal(150, 250),
                    Touranments = f.Random.Int(1, 20)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}