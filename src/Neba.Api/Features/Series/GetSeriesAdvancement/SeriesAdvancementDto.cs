using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed record SeriesAdvancementDto(
    int Version,
    DateOnly? BackfillAsOf,
    int TargetField,
    IReadOnlyList<StageAdvancementDto> Stages,
    IReadOnlyList<SeriesPositionDto> Positions);

internal sealed record StageAdvancementDto(
    SeriesStageId StageId,
    int Order,
    string Kind,
    string Status,
    bool IsStale,
    bool NeedsSquad,
    DateTimeOffset? ComputedAt,
    DateTimeOffset? OfficialDueAt,
    DateTimeOffset? OfficialAt,
    int? SlotCount,
    int? VacantSlotCount,
    TournamentId TournamentId,
    string TournamentStatus,
    SquadId? ResolvedSquadId,
    IReadOnlyList<FieldEntryDto> Entries,
    IReadOnlyList<AlreadyAdvancedDto> AlreadyAdvanced,
    IReadOnlyList<StageOverrideDto> Overrides,
    IReadOnlyList<SurvivorOutcomeDto> SurvivorOutcomes);

internal sealed record FieldEntryDto(
    BowlerId BowlerId, string BowlerName, SeriesStageId? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride);

internal sealed record AlreadyAdvancedDto(
    BowlerId BowlerId, string BowlerName, int Place, SeriesStageId EarlierStageId, int EarlierStageOrder);

internal sealed record StageOverrideDto(
    StageOverrideId OverrideId,
    BowlerId BowlerId,
    string BowlerName,
    string Kind,
    string Reason,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    bool Applied);

internal sealed record SurvivorOutcomeDto(BowlerId BowlerId, string BowlerName, int? BlockTotal, bool Advanced);

internal sealed record SeriesPositionDto(BowlerId BowlerId, string BowlerName, int Position, bool Bowled, bool? IsFinalist);