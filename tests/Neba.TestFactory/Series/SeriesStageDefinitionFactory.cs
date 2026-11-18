using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

public static class SeriesStageDefinitionFactory
{
    public const int ValidAdvancingCount = 8;

    /// <summary>A Qualifying Stage advancing 8. Pass a null count for a Placement Stage, which has none.</summary>
    public static SeriesStageDefinition Create(
        SeriesStageId? stageId = null,
        StageKind? kind = null,
        TournamentId? tournamentId = null,
        int? advancingCount = ValidAdvancingCount,
        DateOnly? squadDate = null,
        TimeOnly? squadStartTime = null)
        => new()
        {
            StageId = stageId,
            Kind = kind ?? StageKind.Qualifying,
            TournamentId = tournamentId ?? TournamentId.New(),
            AdvancingCount = advancingCount,
            SquadDate = squadDate,
            SquadStartTime = squadStartTime,
        };
}