using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class PointsPerTournamentDtoFactory
{
    public const int ValidPoints = 500;
    public const int ValidTournaments = 10;
    public const decimal ValidPointsPerTournament = 50.00m;

    public static PointsPerTournamentDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? points = null,
        int? tournaments = null,
        decimal? pointsPerTournament = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            Points = points ?? ValidPoints,
            Tournaments = tournaments ?? ValidTournaments,
            PointsPerTournament = pointsPerTournament ?? ValidPointsPerTournament
        };

    public static IReadOnlyCollection<PointsPerTournamentDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<PointsPerTournamentDto>()
            .CustomInstantiator(f => new PointsPerTournamentDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                Points = f.Random.Int(50, 1000),
                Tournaments = f.Random.Int(1, 15),
                PointsPerTournament = f.Random.Decimal(10, 150)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}