using Bogus;

using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database.Entities;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class HistoricalTournamentResultFactory
{
    public const decimal ValidPrizeMoney = 500m;

    internal static HistoricalTournamentResult Create(
        Bowler? bowler = null,
        Tournament? tournament = null,
        int? place = null,
        decimal? prizeMoney = null,
        SideCut? sideCut = null)
    {
        var sideCutToUse = sideCut;

        return new()
        {
            Bowler = bowler ?? BowlerFactory.Create(),
            Tournament = tournament ?? TournamentFactory.Create(),
            Place = place,
            PrizeMoney = prizeMoney ?? ValidPrizeMoney,
            SideCut = sideCutToUse,
        };
    }

    internal static IReadOnlyCollection<HistoricalTournamentResult> Bogus(
        int count,
        IReadOnlyCollection<Bowler>? bowlers = null,
        IReadOnlyCollection<Tournament>? tournaments = null,
        int? seed = null)
    {
        var bowlersToUse = bowlers?.ToArray() ?? [.. BowlerFactory.Bogus(count, seed)];
        var tournamentsToUse = tournaments?.ToArray() ?? [.. TournamentFactory.Bogus(count, seed)];

        var faker = new Faker<HistoricalTournamentResult>()
            .CustomInstantiator(f => new HistoricalTournamentResult
            {
                Bowler = f.PickRandom(bowlersToUse),
                Tournament = f.PickRandom(tournamentsToUse),
                Place = f.Random.Bool() ? f.Random.Int(1, 20) : null,
                PrizeMoney = f.Random.Decimal(0, 10000),
                SideCutId = null,
                SideCut = null,
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}