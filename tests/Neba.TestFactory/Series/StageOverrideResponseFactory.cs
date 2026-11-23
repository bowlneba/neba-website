using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class StageOverrideResponseFactory
{
    public const string ValidOverrideId = "01JBQ5Y7K3M9N2P4R6T8V0W3B1";
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const string ValidKind = "Add";
    public const string ValidReason = "Withdrew before the Stage.";
    public const string ValidCreatedBy = "01JTESTADMIN000000000000AA";
    public const bool ValidApplied = true;
    public static readonly DateTimeOffset ValidCreatedAt = new(2025, 3, 25, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ReferenceDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static StageOverrideResponse Create(
        string? overrideId = null,
        string? bowlerId = null,
        string? bowlerName = null,
        string? kind = null,
        string? reason = null,
        string? createdBy = null,
        DateTimeOffset? createdAt = null,
        bool? applied = null)
        => new()
        {
            OverrideId = overrideId ?? ValidOverrideId,
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            Kind = kind ?? ValidKind,
            Reason = reason ?? ValidReason,
            CreatedBy = createdBy ?? ValidCreatedBy,
            CreatedAt = createdAt ?? ValidCreatedAt,
            Applied = applied ?? ValidApplied,
        };

    internal static IReadOnlyList<StageOverrideResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            overrideId: Ulid.BogusString(faker),
            bowlerId: Ulid.BogusString(faker),
            bowlerName: faker.Name.FullName(),
            kind: faker.PickRandom("Add", "Clear"),
            reason: faker.Lorem.Sentence(),
            createdBy: Ulid.BogusString(faker),
            createdAt: ReferenceDate.AddMinutes(-faker.Random.Int(1, 500_000)),
            applied: faker.Random.Bool()))];
    }

    public static IReadOnlyList<StageOverrideResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}