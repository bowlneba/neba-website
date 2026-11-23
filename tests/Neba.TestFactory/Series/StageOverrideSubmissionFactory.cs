using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class StageOverrideSubmissionFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const string ValidReason = "Withdrew before the Stage.";

    internal static StageOverrideSubmission Create(
        string? bowlerId = null,
        string? bowlerName = null,
        string? reason = null)
        => new(bowlerId ?? ValidBowlerId, bowlerName ?? ValidBowlerName, reason ?? ValidReason);

    internal static IReadOnlyList<StageOverrideSubmission> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            bowlerId: Ulid.BogusString(faker),
            bowlerName: faker.Name.FullName(),
            reason: faker.Lorem.Sentence()))];
    }
}