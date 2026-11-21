using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class FieldEntryResponseFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const string ValidSourceStageId = "01JBQ5Y7K3M9N2P4R6T8V0W1X5";
    public const int ValidPlace = 1;
    public const int ValidBackfillPoints = 100;
    public const bool ValidViaOverride = false;

    /// <summary>A Backfill entry (no source Stage, no place) by default. Pass a source Stage for a Stage entry (place defaults to 1).</summary>
    public static FieldEntryResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        string? sourceStageId = null,
        int? place = null,
        int? backfillPoints = null,
        bool? viaOverride = null)
    {
        var isBackfill = sourceStageId is null;

        return new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            SourceStageId = sourceStageId,
            Place = place ?? (isBackfill ? null : ValidPlace),
            BackfillPoints = backfillPoints ?? (isBackfill ? ValidBackfillPoints : null),
            ViaOverride = viaOverride ?? ValidViaOverride,
        };
    }

    internal static IReadOnlyList<FieldEntryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var isBackfill = faker.Random.Bool();

            return Create(
                bowlerId: Ulid.BogusString(faker),
                bowlerName: faker.Name.FullName(),
                sourceStageId: isBackfill ? null : Ulid.BogusString(faker),
                place: isBackfill ? null : faker.Random.Int(1, 40),
                backfillPoints: isBackfill ? faker.Random.Int(1, 500) : null,
                viaOverride: !isBackfill && faker.Random.Bool());
        })];
    }

    public static IReadOnlyList<FieldEntryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}