using Bogus;

using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class PointsPerTournamentResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000010";
    public const string ValidBowlerName = "Jane Smith";
    public const int ValidPoints = 320;
    public const int ValidTournaments = 10;
    public const decimal ValidPointsPerTournament = 32.0m;

    public static PointsPerTournamentResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        int? points = null,
        int? tournaments = null,
        decimal? pointsPerTournament = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments,
            PointsPerTournament = pointsPerTournament ?? ValidPointsPerTournament
        };

    public static IReadOnlyCollection<PointsPerTournamentResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<PointsPerTournamentResponse>()
            .CustomInstantiator(f =>
            {
                var points = f.Random.Int(50, 500);
                var tournaments = f.Random.Int(1, 15);
                return new PointsPerTournamentResponse
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    Points = points,
                    Tournaments = tournaments,
                    PointsPerTournament = Math.Round((decimal)points / tournaments, 2)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}