using Neba.Api.Database.Entities;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class HistoricalTournamentChampionFactory
{
    internal static HistoricalTournamentChampion Create(
        Bowler? bowler = null,
        Tournament? tournament = null)
            => new()
            {
                Bowler = bowler ?? BowlerFactory.Create(),
                Tournament = tournament ?? TournamentFactory.Create()
            };

    internal static IReadOnlyCollection<HistoricalTournamentChampion> Bogus(
        int count,
        Faker faker,
        IReadOnlyCollection<Bowler>? bowlers = null,
        IReadOnlyCollection<Tournament>? tournaments = null)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var bowlersToUse = bowlers?.ToArray() ?? [.. BowlerFactory.Bogus(count, faker)];
        var tournamentsToUse = tournaments?.ToArray() ?? [.. TournamentFactory.Bogus(count, faker)];

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var bowler = faker.PickRandom(bowlersToUse);
            var tournament = faker.PickRandom(tournamentsToUse);
            return Create(bowler, tournament);
        })];
    }

    internal static IReadOnlyCollection<HistoricalTournamentChampion> Bogus(
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