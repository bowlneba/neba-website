using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class PointsPerTournamentRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const int ValidPoints = 100;
    public const int ValidTournaments = 10;

    public static PointsPerTournamentRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? tournaments = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments
        };

    public static IReadOnlyCollection<PointsPerTournamentRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        const int tournaments = 10;

        var faker = new Faker<PointsPerTournamentRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;
                var points = Math.Max(0, count - currentRank + 1);

                return new PointsPerTournamentRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.Bogus(f),
                    BowlerName = f.Name.FullName(),
                    Points = points,
                    Tournaments = tournaments
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
