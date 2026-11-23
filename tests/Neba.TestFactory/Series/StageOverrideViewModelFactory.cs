using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class StageOverrideViewModelFactory
{
    public const string ValidOverrideId = "01JBQ5Y7K3M9N2P4R6T8V0W3B1";
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const string ValidKind = "Add";
    public const string ValidReason = "Withdrew before the Stage.";
    public static readonly DateTimeOffset ValidCreatedAt = new(2025, 3, 25, 8, 0, 0, TimeSpan.FromHours(-4));

    internal static StageOverrideViewModel Create(
        string? overrideId = null,
        string? bowlerId = null,
        string? bowlerName = null,
        string? kind = null,
        string? reason = null,
        DateTimeOffset? createdAt = null,
        bool? applied = null)
        => new(
            overrideId ?? ValidOverrideId,
            bowlerId ?? ValidBowlerId,
            bowlerName ?? ValidBowlerName,
            kind ?? ValidKind,
            reason ?? ValidReason,
            createdAt ?? ValidCreatedAt,
            applied ?? true);

    internal static IReadOnlyList<StageOverrideViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            overrideId: Ulid.BogusString(faker),
            bowlerId: Ulid.BogusString(faker),
            bowlerName: faker.Name.FullName(),
            kind: faker.PickRandom(OverrideKinds.Add, OverrideKinds.Clear),
            reason: faker.Lorem.Sentence(),
            createdAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(-faker.Random.Int(1, 500_000)),
            applied: faker.Random.Bool()))];
    }
}