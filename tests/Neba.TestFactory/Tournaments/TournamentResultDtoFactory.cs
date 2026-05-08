

using Neba.Application.Tournaments.GetTournament;
using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class TournamentResultDtoFactory
{
    public static TournamentResultDto Create(
        Name? bowlerName = null,
        int? place = null,
        decimal? prizeMoney = null,
        int? points = null,
        SideCut? sideCut = null)
        => new()
        {
            BowlerName = bowlerName ?? NameFactory.Create(),
            Place = place,
            PrizeMoney = prizeMoney ?? 50,
            Points = points ?? 5,
            SideCutName = sideCut?.Name,
            SideCutIndicator = sideCut?.Indicator
        };

    public static IReadOnlyCollection<TournamentResultDto> Bogus(int count, int? seed = null)
    {
        var names = UniquePool.Create(NameFactory.Bogus(count, seed), seed);
        var sideCuts = UniquePool.CreateNullable(SideCutFactory.Bogus(count, seed), seed, .2f);
        var place = UniquePool.CreateNullable(Enumerable.Range(1, count), seed);

        var faker = new Faker<TournamentResultDto>()
            .CustomInstantiator(f =>
            {
                var sideCut = sideCuts.GetNextNullable();

                return new()
                {
                    BowlerName = names.GetNext(),
                    Place = place.GetNextNullable(),
                    PrizeMoney = f.Random.Decimal(0, 1000),
                    Points = f.Random.Int(0, 100),
                    SideCutName = sideCut?.Name,
                    SideCutIndicator = sideCut?.Indicator
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}