using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Database.Entities;
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
        IReadOnlyCollection<Bowler>? bowlers = null,
        IReadOnlyCollection<Tournament>? tournaments = null,
        int? seed = null)
    {
        var bowlersToUse = bowlers?.ToArray() ?? [.. BowlerFactory.Bogus(count, seed)];
        var tournamentsToUse = tournaments?.ToArray() ?? [.. TournamentFactory.Bogus(count, seed)];

        var faker = new Faker<HistoricalTournamentChampion>()
            .CustomInstantiator(faker =>
            {
                var bowler = faker.PickRandom(bowlersToUse);
                var tournament = faker.PickRandom(tournamentsToUse);

                return Create(bowler, tournament);
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}