using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class FieldEntryViewModelFactory
{
    public const string ValidBowlerId = "01JBQ5Y7K3M9N2P4R6T8V0W2A1";
    public const string ValidBowlerName = "Dave Smith";
    public const string ValidSourceStageId = "01JBQ5Y7K3M9N2P4R6T8V0W1X5";
    public const int ValidPlace = 3;

    /// <summary>A Qualifier entry from the default Stage: third place, no override, no Clear. Pass <paramref name="isBackfill"/> for a Backfill entry.</summary>
    internal static FieldEntryViewModel Create(
        string? bowlerId = null,
        string? bowlerName = null,
        string? sourceStageId = null,
        int? place = null,
        int? backfillPoints = null,
        bool viaOverride = false,
        bool canClear = false,
        bool isBackfill = false)
        => new(
            bowlerId ?? ValidBowlerId,
            bowlerName ?? ValidBowlerName,
            isBackfill ? null : sourceStageId ?? ValidSourceStageId,
            isBackfill ? null : place ?? ValidPlace,
            isBackfill ? backfillPoints ?? 120 : backfillPoints,
            viaOverride,
            canClear);

    internal static IReadOnlyList<FieldEntryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(index => Create(
            bowlerId: Ulid.BogusString(faker),
            bowlerName: faker.Name.FullName(),
            sourceStageId: Ulid.BogusString(faker),
            place: index + 1,
            isBackfill: index % 4 == 3))];
    }
}