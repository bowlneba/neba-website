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
        string? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? tournaments = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments
        };

    public static IReadOnlyCollection<PointsPerTournamentRowViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var rank = 1;
        const int tournaments = 10;
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var currentRank = rank++;
            var points = Math.Max(0, count - currentRank + 1);
            return new PointsPerTournamentRowViewModel
            {
                Rank = currentRank,
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                Points = points,
                Tournaments = tournaments
            };
        })];
    }

    public static IReadOnlyCollection<PointsPerTournamentRowViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}