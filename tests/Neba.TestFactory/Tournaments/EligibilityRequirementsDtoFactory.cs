using Bogus;

using Neba.Api.Features.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibilityRequirementsDtoFactory
{
    public const int ValidMinTournamentsCount = 3;

    internal static EligibilityRequirementsDto Create(int? minTournamentsCount = ValidMinTournamentsCount)
        => new() { MinTournamentsCount = minTournamentsCount };

    internal static IReadOnlyCollection<EligibilityRequirementsDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(faker.Random.Int(1, 10)))];
    }
}