using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class SeriesFieldEntryFactory
{
    public const int ValidPlace = 1;
    public const int ValidBackfillPoints = 100;

    /// <summary>
    /// A Backfill entry with no source Stage, so it persists with only a Series and a bowler. Pass a source Stage for a
    /// Stage entry (place defaults to 1).
    /// </summary>
    public static SeriesFieldEntry Create(
        BowlerId? bowlerId = null,
        SeriesStageId? sourceStageId = null,
        int? place = null,
        int? backfillPoints = null,
        bool viaOverride = false)
    {
        var isBackfill = sourceStageId is null;

        var result = SeriesFieldEntry.Create(
            bowlerId ?? BowlerId.New(),
            sourceStageId,
            place ?? (isBackfill ? null : ValidPlace),
            backfillPoints ?? (isBackfill ? ValidBackfillPoints : null),
            viaOverride);

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create field entry: {result.Errors[0].Description}")
            : result.Value;
    }

    /// <summary>Stage entries placed 1..count in one source Stage, each a new bowler.</summary>
    public static IReadOnlyCollection<SeriesFieldEntry> Bogus(int count, SeriesStageId? sourceStageId = null, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(1, count).Select(place => Create(
            bowlerId: new BowlerId(Ulid.BogusString(faker)),
            sourceStageId: sourceStageId,
            place: sourceStageId is null ? null : place,
            backfillPoints: sourceStageId is null ? faker.Random.Int(1, 500) : null))];
    }
}