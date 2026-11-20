using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

internal static class StageOverrideDtoFactory
{
    public const string ValidBowlerName = "Dave Smith";
    public const string ValidKind = "Add";
    public const string ValidReason = "Withdrew before the Stage.";
    public const string ValidCreatedBy = "01JTESTADMIN000000000000AA";
    public const bool ValidApplied = true;
    public static readonly DateTimeOffset ValidCreatedAt = new(2025, 3, 25, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ReferenceDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static StageOverrideDto Create(
        StageOverrideId? overrideId = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        string? kind = null,
        string? reason = null,
        string? createdBy = null,
        DateTimeOffset? createdAt = null,
        bool? applied = null)
        => new(
            overrideId ?? StageOverrideId.New(),
            bowlerId ?? BowlerId.New(),
            bowlerName ?? ValidBowlerName,
            kind ?? ValidKind,
            reason ?? ValidReason,
            createdBy ?? ValidCreatedBy,
            createdAt ?? ValidCreatedAt,
            applied ?? ValidApplied);

    internal static IReadOnlyList<StageOverrideDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            overrideId: new StageOverrideId(Ulid.BogusString(faker)),
            bowlerId: new BowlerId(Ulid.BogusString(faker)),
            bowlerName: faker.Name.FullName(),
            kind: faker.PickRandom("Add", "Clear"),
            reason: faker.Lorem.Sentence(),
            createdBy: Ulid.BogusString(faker),
            createdAt: ReferenceDate.AddMinutes(-faker.Random.Int(1, 500_000)),
            applied: faker.Random.Bool()))];
    }

    public static IReadOnlyList<StageOverrideDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}