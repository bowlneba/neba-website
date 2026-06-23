using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
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

    public static IReadOnlyCollection<PointsPerTournamentDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new PointsPerTournamentDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            Points = faker.Random.Int(50, 1000),
            Tournaments = faker.Random.Int(1, 15),
            PointsPerTournament = faker.Random.Decimal(10, 150)
        })];
    }

    public static IReadOnlyCollection<PointsPerTournamentDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}