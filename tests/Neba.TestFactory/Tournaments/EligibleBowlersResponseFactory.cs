using Bogus;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlersResponseFactory
{
    public static EligibleBowlersResponse Create(
        EligibilityRequirementsResponse? requirements = null,
        IReadOnlyCollection<EligibleBowlerSummaryResponse>? bowlers = null)
        => new()
        {
            Requirements = requirements ?? EligibilityRequirementsResponseFactory.Create(),
            Bowlers = bowlers ?? [EligibleBowlerSummaryResponseFactory.Create()]
        };

    public static EligibleBowlersResponse CreateNotApplicable()
        => new() { Requirements = null, Bowlers = [] };

    public static IReadOnlyCollection<EligibleBowlersResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            bowlers: EligibleBowlerSummaryResponseFactory.Bogus(3, faker)))];
    }
}
