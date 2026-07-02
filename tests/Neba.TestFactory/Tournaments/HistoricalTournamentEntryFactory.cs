using Neba.Api.Database.Entities;
using Neba.Api.Features.Tournaments.Domain;

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
        Faker faker,
        IReadOnlyCollection<Tournament>? tournaments = null)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var tournamentPool = UniquePool.Create(
            tournaments?.ToArray() ?? [.. TournamentFactory.Bogus(count, faker)],
            poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ =>
            Create(tournamentPool.GetNext(), faker.Random.Int(150, 300)))];
    }

    internal static IReadOnlyCollection<HistoricalTournamentEntry> Bogus(
        int count,
        IReadOnlyCollection<Tournament>? tournaments = null,
        int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker, tournaments);
    }
}