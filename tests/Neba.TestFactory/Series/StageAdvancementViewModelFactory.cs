using Neba.Website.Server.Series;

namespace Neba.TestFactory.Series;

public static class StageAdvancementViewModelFactory
{
    public const string ValidId = "01JBQ5Y7K3M9N2P4R6T8V0W1X5";
    public const string ValidTournamentId = "01JBQ5Y7K3M9N2P4R6T8V0W5D1";
    public const string ValidResolvedSquadId = "01JBQ5Y7K3M9N2P4R6T8V0W4C1";
    public const int ValidOrder = 1;
    public const string ValidKind = "Qualifying";
    public const string ValidStatus = "Pending";
    public const string ValidTournamentStatus = "Scheduled";
    public const int ValidSlotCount = 8;

    /// <summary>
    /// A Pending Qualifying Stage over a Scheduled tournament with no output and no actions.
    /// The kind picks the defaults: Qualifying has a slot count and one entry, Survivor has a resolved squad and one outcome.
    /// Pass an empty list to clear a default collection.
    /// </summary>
    internal static StageAdvancementViewModel Create(
        string? id = null,
        int? order = null,
        string? kind = null,
        string? status = null,
        bool isStale = false,
        bool needsSquad = false,
        DateTimeOffset? officialDueAt = null,
        int? slotCount = null,
        int? vacantSlotCount = null,
        string? tournamentId = null,
        string? tournamentStatus = null,
        string? resolvedSquadId = null,
        bool isLastQualifying = false,
        IReadOnlyList<FieldEntryViewModel>? entries = null,
        IReadOnlyList<StageOverrideViewModel>? overrides = null,
        IReadOnlyList<SurvivorOutcomeViewModel>? outcomes = null,
        bool canAddOverride = false,
        bool canRemoveOverride = false,
        bool canRecompute = false,
        bool canPickSquad = false)
    {
        var stageKind = kind ?? ValidKind;
        var isQualifying = stageKind == StageKinds.Qualifying;
        var isSurvivor = stageKind == StageKinds.Survivor;
        var stageId = id ?? ValidId;

        return new(
            Id: stageId,
            Order: order ?? ValidOrder,
            Kind: stageKind,
            Status: status ?? ValidStatus,
            IsStale: isStale,
            NeedsSquad: needsSquad,
            OfficialDueAt: officialDueAt,
            SlotCount: slotCount ?? (isQualifying ? ValidSlotCount : null),
            VacantSlotCount: vacantSlotCount ?? (isQualifying ? 0 : null),
            TournamentId: tournamentId ?? ValidTournamentId,
            TournamentStatus: tournamentStatus ?? ValidTournamentStatus,
            ResolvedSquadId: resolvedSquadId ?? (isSurvivor ? ValidResolvedSquadId : null),
            IsLastQualifying: isLastQualifying,
            Entries: entries ?? (isQualifying ? [FieldEntryViewModelFactory.Create(sourceStageId: stageId)] : []),
            Overrides: overrides ?? [],
            Outcomes: outcomes ?? (isSurvivor ? [SurvivorOutcomeViewModelFactory.Create()] : []),
            CanAddOverride: canAddOverride,
            CanRemoveOverride: canRemoveOverride,
            CanRecompute: canRecompute,
            CanPickSquad: canPickSquad);
    }

    /// <summary>Stages 1..count cycling Qualifying, Survivor, Placement. Computed implies a due time; a Placement Stage is never Official.</summary>
    internal static IReadOnlyList<StageAdvancementViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        string[] kinds = [StageKinds.Qualifying, StageKinds.Survivor, StageKinds.Placement];
        var reference = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return [.. Enumerable.Range(0, count).Select(index =>
        {
            var kind = kinds[index % kinds.Length];
            var stageId = Ulid.BogusString(faker);
            var status = kind == StageKinds.Placement
                ? faker.PickRandom(StageStatuses.Pending, StageStatuses.Computed)
                : faker.PickRandom(StageStatuses.Pending, StageStatuses.Computed, StageStatuses.Official);

            return Create(
                id: stageId,
                order: index + 1,
                kind: kind,
                status: status,
                isStale: status != StageStatuses.Pending && faker.Random.Bool(),
                needsSquad: kind == StageKinds.Survivor && status == StageStatuses.Pending && faker.Random.Bool(),
                officialDueAt: status == StageStatuses.Computed && kind != StageKinds.Placement
                    ? reference.AddHours(faker.Random.Int(1, 48))
                    : null,
                slotCount: kind == StageKinds.Qualifying ? faker.Random.Int(1, 40) : null,
                vacantSlotCount: kind == StageKinds.Qualifying ? faker.Random.Int(0, 3) : null,
                tournamentId: Ulid.BogusString(faker),
                tournamentStatus: status == StageStatuses.Pending ? "Scheduled" : "Completed",
                resolvedSquadId: kind == StageKinds.Survivor ? Ulid.BogusString(faker) : null,
                entries: status != StageStatuses.Pending && kind == StageKinds.Qualifying
                    ? FieldEntryViewModelFactory.Bogus(faker.Random.Int(1, 3), faker.Random.Int())
                    : [],
                overrides: status != StageStatuses.Pending && kind == StageKinds.Qualifying
                    ? StageOverrideViewModelFactory.Bogus(faker.Random.Int(0, 2), faker.Random.Int())
                    : [],
                outcomes: status != StageStatuses.Pending && kind == StageKinds.Survivor
                    ? SurvivorOutcomeViewModelFactory.Bogus(faker.Random.Int(1, 3), faker.Random.Int())
                    : [],
                canRecompute: true);
        })];
    }
}