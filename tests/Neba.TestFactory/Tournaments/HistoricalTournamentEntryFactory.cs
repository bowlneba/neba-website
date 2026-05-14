using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database.Entities;

namespace Neba.TestFactory.Tournaments;

public static class HistoricalTournamentEntryFactory
{
    internal static HistoricalTournamentEntry Create(
        Tournament? tournament = null,
        int? entries = null)
            => new()
            {
                Tournament = tournament ?? TournamentFactory.Create(),
                Entries = entries ?? 200
            };

    internal static IReadOnlyCollection<HistoricalTournamentEntry> Bogus(
        int count,
        IReadOnlyCollection<Tournament>? tournaments = null,
        int? seed = null)
    {
        var tournamentPool = UniquePool.Create(tournaments?.ToArray() ?? [.. TournamentFactory.Bogus(count, seed)], seed);

        var faker = new Faker<HistoricalTournamentEntry>()
            .CustomInstantiator(f => Create(tournamentPool.GetNext(), f.Random.Int(150, 300)));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}