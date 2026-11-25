using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.TestFactory.Series;

public static class StageAdvancementResponseFactory
{
    public const string ValidStageId = "01JBQ5Y7K3M9N2P4R6T8V0W1X5";
    public const string ValidTournamentId = "01JBQ5Y7K3M9N2P4R6T8V0W5D1";
    public const string ValidResolvedSquadId = "01JBQ5Y7K3M9N2P4R6T8V0W4C1";
    public const int ValidOrder = 1;
    public const string ValidKind = "Qualifying";
    public const string ValidStatus = "Pending";
    public const bool ValidIsStale = false;
    public const bool ValidNeedsSquad = false;
    public const string ValidTournamentStatus = "Scheduled";
    public const int ValidSlotCount = 8;
    public const int ValidVacantSlotCount = 0;

    private static readonly DateTimeOffset ReferenceDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A Pending Stage over a Scheduled tournament. The kind picks the defaults: Qualifying has a slot count and
    /// one Backfill entry, Survivor has a resolved squad and one outcome, Placement has none of these.
    /// Pass an empty list to clear a default collection.
    /// </summary>
    public static StageAdvancementResponse Create(
        string? stageId = null,
        int? order = null,
        string? kind = null,
        string? status = null,
        bool? isStale = null,
        bool? needsSquad = null,
        DateTimeOffset? computedAt = null,
        bool? canMakeOfficial = null,
        DateTimeOffset? officialAt = null,
        int? slotCount = null,
        int? vacantSlotCount = null,
        string? tournamentId = null,
        string? tournamentStatus = null,
        string? resolvedSquadId = null,
        IReadOnlyList<FieldEntryResponse>? entries = null,
        IReadOnlyList<StageOverrideResponse>? overrides = null,
        IReadOnlyList<SurvivorOutcomeResponse>? survivorOutcomes = null,
        IReadOnlyList<AlreadyAdvancedResponse>? alreadyAdvanced = null)
    {
        var stageKind = kind ?? ValidKind;
        var isQualifying = stageKind == "Qualifying";
        var isSurvivor = stageKind == "Survivor";

        return new()
        {
            StageId = stageId ?? ValidStageId,
            Order = order ?? ValidOrder,
            Kind = stageKind,
            Status = status ?? ValidStatus,
            IsStale = isStale ?? ValidIsStale,
            NeedsSquad = needsSquad ?? ValidNeedsSquad,
            ComputedAt = computedAt,
            CanMakeOfficial = canMakeOfficial ?? false,
            OfficialAt = officialAt,
            SlotCount = slotCount ?? (isQualifying ? ValidSlotCount : null),
            VacantSlotCount = vacantSlotCount ?? (isQualifying ? ValidVacantSlotCount : null),
            TournamentId = tournamentId ?? ValidTournamentId,
            TournamentStatus = tournamentStatus ?? ValidTournamentStatus,
            ResolvedSquadId = resolvedSquadId ?? (isSurvivor ? ValidResolvedSquadId : null),
            Entries = entries ?? (isQualifying ? [FieldEntryResponseFactory.Create()] : []),
            AlreadyAdvanced = alreadyAdvanced ?? [],
            Overrides = overrides ?? [],
            SurvivorOutcomes = survivorOutcomes ?? (isSurvivor ? [SurvivorOutcomeResponseFactory.Create()] : []),
        };
    }

    /// <summary>Stages 1..count cycling Qualifying, Survivor, Placement; each has output of its kind.</summary>
    internal static IReadOnlyList<StageAdvancementResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        string[] kinds = ["Qualifying", "Survivor", "Placement"];

        return [.. Enumerable.Range(0, count).Select(index =>
        {
            var kind = kinds[index % kinds.Length];
            var computedAt = ReferenceDate.AddMinutes(-faker.Random.Int(1, 500_000));

            return Create(
                stageId: Ulid.BogusString(faker),
                order: index + 1,
                kind: kind,
                status: faker.PickRandom("Computed", "Official"),
                isStale: faker.Random.Bool(),
                needsSquad: kind == "Survivor" && faker.Random.Bool(),
                computedAt: computedAt,
                canMakeOfficial: false,
                officialAt: faker.Random.Bool() ? computedAt.AddHours(25) : null,
                slotCount: kind == "Qualifying" ? faker.Random.Int(1, 40) : null,
                vacantSlotCount: kind == "Qualifying" ? faker.Random.Int(0, 3) : null,
                tournamentId: Ulid.BogusString(faker),
                tournamentStatus: faker.PickRandom("Completed", "Truncated"),
                resolvedSquadId: kind == "Survivor" ? Ulid.BogusString(faker) : null,
                entries: kind == "Qualifying" ? FieldEntryResponseFactory.Bogus(faker.Random.Int(1, 3), faker) : [],
                overrides: kind == "Qualifying" ? StageOverrideResponseFactory.Bogus(faker.Random.Int(1, 2), faker) : [],
                survivorOutcomes: kind == "Survivor" ? SurvivorOutcomeResponseFactory.Bogus(faker.Random.Int(1, 3), faker) : []);
        })];
    }

    public static IReadOnlyList<StageAdvancementResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}