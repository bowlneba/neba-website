using Bogus;

using Neba.Api.Contracts.Tournaments.ListTournamentTypes;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class TournamentTypeSummaryResponseFactory
{
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static TournamentTypeSummaryResponse Create(TournamentType? tournamentType = null)
        => new()
        {
            Name = (tournamentType ?? ValidTournamentType).Name
        };

    internal static IReadOnlyCollection<TournamentTypeSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new TournamentTypeSummaryResponse
        {
            Name = faker.PickRandom(TournamentType.List.ToArray()).Name
        })];
    }

    public static IReadOnlyCollection<TournamentTypeSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}