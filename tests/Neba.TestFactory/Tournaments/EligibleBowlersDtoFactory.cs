using Bogus;

using Neba.Api.Features.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlersDtoFactory
{
    internal static EligibleBowlersDto Create(
        EligibilityRequirementsDto? requirements = null,
        IReadOnlyCollection<EligibleBowlerSummaryDto>? bowlers = null)
        => new()
        {
            Requirements = requirements ?? EligibilityRequirementsDtoFactory.Create(),
            Bowlers = bowlers ?? [EligibleBowlerSummaryDtoFactory.Create()]
        };

    internal static EligibleBowlersDto CreateNotApplicable()
        => new() { Requirements = null, Bowlers = [] };

    internal static IReadOnlyCollection<EligibleBowlersDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            bowlers: EligibleBowlerSummaryDtoFactory.Bogus(3, faker)))];
    }
}
