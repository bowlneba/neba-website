using Bogus;

using Neba.Api.Features.Tournaments;
using Neba.Api.Features.Tournaments.CheckTeamEligibility;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Tournaments;

public static class TeamEligibilityDtoFactory
{
    internal static TeamEligibilityDto Create(
        TeamEligibilityOutcome? outcome = null,
        IReadOnlyList<TeamEligibilityReason>? reasons = null,
        IReadOnlyList<TeamEligibilityCheck>? dateChecks = null,
        DefendingTeam? defendingTeam = null,
        TeamEligibilityExplanation? explanation = null,
        IReadOnlyList<TeamEligibilityMemberIdentity>? memberIdentities = null)
        => new(
            outcome ?? TeamEligibilityOutcome.Eligible,
            reasons ?? [],
            dateChecks ?? [],
            defendingTeam,
            explanation,
            memberIdentities);

    internal static IReadOnlyCollection<TeamEligibilityDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            outcome: faker.PickRandom(TeamEligibilityOutcome.List.ToArray())))];
    }

    internal static IReadOnlyCollection<TeamEligibilityDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}