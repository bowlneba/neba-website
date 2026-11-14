using Bogus;

using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;

namespace Neba.TestFactory.Tournaments;

public static class TeamEligibilityResponseFactory
{
    public const string ValidOutcome = "Eligible";
    public const string ValidSummary = "The team can bowl together.";

    public static TeamEligibilityResponse Create(
        string? outcome = null,
        IReadOnlyCollection<TeamEligibilityReasonResponse>? reasons = null,
        IReadOnlyCollection<TeamEligibilityDateCheckResponse>? dateChecks = null,
        bool? professionalTitlesVerified = null,
        DefendingSourceResponse? defendingSource = null,
        string? summary = null,
        IReadOnlyCollection<TeamEligibilityMemberResponse>? members = null,
        IReadOnlyCollection<TeamEligibilityCheckResponse>? checks = null)
        => new()
        {
            Outcome = outcome ?? ValidOutcome,
            Reasons = reasons ?? [],
            DateChecks = dateChecks ?? [],
            ProfessionalTitlesVerified = professionalTitlesVerified ?? false,
            DefendingSource = defendingSource,
            Summary = summary ?? ValidSummary,
            Members = members ?? [TeamEligibilityMemberResponseFactory.Create()],
            Checks = checks ?? []
        };

    internal static IReadOnlyCollection<TeamEligibilityResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        string[] outcomes = ["DefendingChampions", "Eligible", "Ineligible", "DependsOnDate"];

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            outcome: faker.PickRandom(outcomes),
            summary: faker.Lorem.Sentence(),
            members: [.. TeamEligibilityMemberResponseFactory.Bogus(faker.Random.Int(2, 3), faker)]))];
    }

    public static IReadOnlyCollection<TeamEligibilityResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
