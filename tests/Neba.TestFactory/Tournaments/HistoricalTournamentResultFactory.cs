using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
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
        Faker faker,
        IReadOnlyCollection<Bowler>? bowlers = null,
        IReadOnlyCollection<Tournament>? tournaments = null)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var bowlersToUse = bowlers?.ToArray() ?? [.. BowlerFactory.Bogus(count, faker)];
        var tournamentsToUse = tournaments?.ToArray() ?? [.. TournamentFactory.Bogus(count, faker)];

        return [.. Enumerable.Range(0, count).Select(_ => new HistoricalTournamentResult
        {
            Bowler = faker.PickRandom(bowlersToUse),
            Tournament = faker.PickRandom(tournamentsToUse),
            Place = faker.Random.Bool() ? faker.Random.Int(1, 20) : null,
            PrizeMoney = faker.Random.Decimal(0, 10000),
            SideCutId = null,
            SideCut = null,
        })];
    }

    internal static IReadOnlyCollection<HistoricalTournamentResult> Bogus(
        int count,
        IReadOnlyCollection<Bowler>? bowlers = null,
        IReadOnlyCollection<Tournament>? tournaments = null,
        int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker, bowlers, tournaments);
    }
}