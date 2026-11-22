using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

internal static class FieldEntryDtoFactory
{
    public const string ValidBowlerName = "Dave Smith";
    public const int ValidPlace = 1;
    public const int ValidBackfillPoints = 100;
    public const bool ValidViaOverride = false;

    /// <summary>
    /// A Backfill entry (no source Stage, no place) by default. Pass a source Stage for a Stage entry (place defaults to 1).
    /// </summary>
    public static FieldEntryDto Create(
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        SeriesStageId? sourceStageId = null,
        int? place = null,
        int? backfillPoints = null,
        bool? viaOverride = null)
    {
        var isBackfill = sourceStageId is null;

        return new FieldEntryDto(
            bowlerId ?? BowlerId.New(),
            bowlerName ?? ValidBowlerName,
            sourceStageId,
            place ?? (isBackfill ? null : ValidPlace),
            backfillPoints ?? (isBackfill ? ValidBackfillPoints : null),
            viaOverride ?? ValidViaOverride);
    }

    internal static IReadOnlyList<FieldEntryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var isBackfill = faker.Random.Bool();

            return Create(
                bowlerId: new BowlerId(Ulid.BogusString(faker)),
                bowlerName: faker.Name.FullName(),
                sourceStageId: isBackfill ? null : new SeriesStageId(Ulid.BogusString(faker)),
                place: isBackfill ? null : faker.Random.Int(1, 40),
                backfillPoints: isBackfill ? faker.Random.Int(1, 500) : null,
                viaOverride: !isBackfill && faker.Random.Bool());
        })];
    }

    public static IReadOnlyList<FieldEntryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}