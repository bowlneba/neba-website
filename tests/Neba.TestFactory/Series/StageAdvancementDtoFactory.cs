using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.GetSeriesAdvancement;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

internal static class StageAdvancementDtoFactory
{
    public const int ValidOrder = 1;
    public const string ValidKind = "Qualifying";
    public const string ValidStatus = "Pending";
    public const bool ValidIsStale = false;
    public const bool ValidNeedsSquad = false;
    public const string ValidTournamentStatus = "Scheduled";

    private static readonly DateTimeOffset ReferenceDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>A Pending Qualifying Stage over a Scheduled tournament, with no output.</summary>
    public static StageAdvancementDto Create(
        SeriesStageId? stageId = null,
        int? order = null,
        string? kind = null,
        string? status = null,
        bool? isStale = null,
        bool? needsSquad = null,
        DateTimeOffset? computedAt = null,
        DateTimeOffset? officialDueAt = null,
        DateTimeOffset? officialAt = null,
        int? slotCount = null,
        int? vacantSlotCount = null,
        TournamentId? tournamentId = null,
        string? tournamentStatus = null,
        SquadId? resolvedSquadId = null,
        IReadOnlyList<FieldEntryDto>? entries = null,
        IReadOnlyList<StageOverrideDto>? overrides = null,
        IReadOnlyList<SurvivorOutcomeDto>? survivorOutcomes = null)
        => new(
            stageId ?? SeriesStageId.New(),
            order ?? ValidOrder,
            kind ?? ValidKind,
            status ?? ValidStatus,
            isStale ?? ValidIsStale,
            needsSquad ?? ValidNeedsSquad,
            computedAt,
            officialDueAt,
            officialAt,
            slotCount,
            vacantSlotCount,
            tournamentId ?? TournamentId.New(),
            tournamentStatus ?? ValidTournamentStatus,
            resolvedSquadId,
            entries ?? [],
            overrides ?? [],
            survivorOutcomes ?? []);

    /// <summary>Stages 1..count cycling Qualifying, Survivor, Placement; each has output of its kind.</summary>
    internal static IReadOnlyList<StageAdvancementDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        string[] kinds = ["Qualifying", "Survivor", "Placement"];

        return [.. Enumerable.Range(0, count).Select(index =>
        {
            var kind = kinds[index % kinds.Length];
            var computedAt = ReferenceDate.AddMinutes(-faker.Random.Int(1, 500_000));

            return Create(
                stageId: new SeriesStageId(Ulid.BogusString(faker)),
                order: index + 1,
                kind: kind,
                status: faker.PickRandom("Computed", "Official"),
                isStale: faker.Random.Bool(),
                needsSquad: kind == "Survivor" && faker.Random.Bool(),
                computedAt: computedAt,
                officialDueAt: computedAt.AddHours(24),
                officialAt: faker.Random.Bool() ? computedAt.AddHours(25) : null,
                slotCount: kind == "Qualifying" ? faker.Random.Int(1, 40) : null,
                vacantSlotCount: kind == "Qualifying" ? faker.Random.Int(0, 3) : null,
                tournamentId: new TournamentId(Ulid.BogusString(faker)),
                tournamentStatus: faker.PickRandom("Completed", "Truncated"),
                resolvedSquadId: kind == "Survivor" ? new SquadId(Ulid.BogusString(faker)) : null,
                entries: kind == "Qualifying" ? FieldEntryDtoFactory.Bogus(faker.Random.Int(1, 3), faker) : [],
                overrides: kind == "Qualifying" ? StageOverrideDtoFactory.Bogus(faker.Random.Int(1, 2), faker) : [],
                survivorOutcomes: kind == "Survivor" ? SurvivorOutcomeDtoFactory.Bogus(faker.Random.Int(1, 3), faker) : []);
        })];
    }

    public static IReadOnlyList<StageAdvancementDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}