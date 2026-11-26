using Bogus;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibilityRequirementsResponseFactory
{
    public const int ValidMinTournamentsCount = 3;

    public static EligibilityRequirementsResponse Create(int? minTournamentsCount = ValidMinTournamentsCount)
        => new() { MinTournamentsCount = minTournamentsCount };

    public static IReadOnlyCollection<EligibilityRequirementsResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(faker.Random.Int(1, 10)))];
    }
}