using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.ListStageSquads;

internal sealed record StageSquadSummaryDto(
    SquadId SquadId, DateOnly LocalDate, TimeOnly LocalStartTime, bool HasScores, bool IsSelected);