using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

internal sealed record FieldEntryDraft(BowlerId BowlerId, SeriesStageId? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride);

internal sealed record SurvivorOutcomeDraft(BowlerId BowlerId, int? BlockTotal, bool Advanced);

internal sealed record PositionDraft(BowlerId BowlerId, int Position, bool Bowled, bool? IsFinalist);

internal sealed record OverrideEffect(StageOverrideId OverrideId, bool Applied);

/// <summary>A Stage's computed output. A pure function of earlier Stages' stored output, the results, the overrides and the facts.</summary>
internal sealed record StageComputation
{
    public IReadOnlyList<FieldEntryDraft> Entries { get; init; } = [];

    public int? SlotCount { get; init; }

    public int Vacancies { get; init; }

    public IReadOnlyList<OverrideEffect> OverrideEffects { get; init; } = [];

    public IReadOnlyList<FieldEntryDraft> BackfillEntries { get; init; } = [];

    public DateOnly? BackfillAsOf { get; init; }

    public IReadOnlyList<SurvivorOutcomeDraft> Outcomes { get; init; } = [];

    public IReadOnlyList<PositionDraft> Positions { get; init; } = [];
}