using Bogus;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Database.Entities;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class HistoricalTournamentResultFactory
{
    public const decimal ValidPrizeMoney = 500m;

    public const int ValidPoints = 100;

    internal static HistoricalTournamentResult Create(
        Bowler? bowler = null,
        Tournament? tournament = null,
        int? place = null,
        decimal? prizeMoney = null,
        int? points = null,
        SideCut? sideCut = null)
    {
        var sideCutToUse = sideCut;

        return new()
        {
            Bowler = bowler ?? BowlerFactory.Create(),
            Tournament = tournament ?? TournamentFactory.Create(),
            Place = place,
            PrizeMoney = prizeMoney ?? ValidPrizeMoney,
            Points = points ?? ValidPoints,
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