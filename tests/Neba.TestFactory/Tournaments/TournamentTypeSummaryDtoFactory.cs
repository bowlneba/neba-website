using Bogus;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListTournamentTypes;

namespace Neba.TestFactory.Tournaments;

public static class TournamentTypeSummaryDtoFactory
{
    public static readonly TournamentType ValidTournamentType = TournamentType.Singles;

    public static TournamentTypeSummaryDto Create(TournamentType? tournamentType = null)
        => new()
        {
            Name = (tournamentType ?? ValidTournamentType).Name
        };

    internal static IReadOnlyCollection<TournamentTypeSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new TournamentTypeSummaryDto
        {
            Name = faker.PickRandom(TournamentType.List.ToArray()).Name
        })];
    }

    public static IReadOnlyCollection<TournamentTypeSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}