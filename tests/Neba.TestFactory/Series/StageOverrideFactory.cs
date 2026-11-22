using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class StageOverrideFactory
{
    public const string ValidReason = "Withdrew before the Stage.";
    public const string ValidCreatedBy = "01JTESTADMIN000000000000AA";
    public static readonly DateTimeOffset ValidCreatedAt = new(2025, 3, 25, 12, 0, 0, TimeSpan.Zero);

    /// <summary>An applied Add override. Calls the domain factory, so the invariants hold.</summary>
    public static StageOverride Create(
        OverrideKind? kind = null,
        BowlerId? bowlerId = null,
        string? reason = null,
        string? createdBy = null,
        DateTimeOffset? createdAt = null)
    {
        var result = StageOverride.Create(
            kind ?? OverrideKind.Add,
            bowlerId ?? BowlerId.New(),
            reason ?? ValidReason,
            createdBy ?? ValidCreatedBy,
            createdAt ?? ValidCreatedAt);

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create stage override: {result.Errors[0].Description}")
            : result.Value;
    }

    public static IReadOnlyCollection<StageOverride> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            kind: faker.PickRandom(OverrideKind.Add, OverrideKind.Clear),
            bowlerId: new BowlerId(Ulid.BogusString(faker)),
            reason: faker.Lorem.Sentence(),
            createdAt: faker.Date.RecentOffset(30).ToUniversalTime()))];
    }
}