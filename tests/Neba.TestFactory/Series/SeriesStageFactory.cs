using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

public static class SeriesStageFactory
{
    public const int ValidAdvancingCount = 8;

    /// <summary>
    /// A Pending Qualifying Stage advancing 8, built through a Series. A Survivor Stage gets a squad date and
    /// start time. A Placement Stage has no count.
    /// </summary>
    public static SeriesStage Create(StageKind? kind = null, int? advancingCount = null)
    {
        var stageKind = kind ?? StageKind.Qualifying;
        int? count = stageKind == StageKind.Placement ? null : advancingCount ?? ValidAdvancingCount;
        var tournamentId = TournamentId.New();
        var seasonId = SeasonId.New();

        var definition = SeriesStageDefinitionFactory.Create(
            kind: stageKind,
            tournamentId: tournamentId,
            advancingCount: count,
            squadDate: stageKind == StageKind.Survivor ? StageSquadFactFactory.ValidLocalDate : null,
            squadStartTime: stageKind == StageKind.Survivor ? StageSquadFactFactory.ValidLocalStartTime : null);

        var series = SeriesFactory.Create(
            seasonId: seasonId,
            stages: [definition],
            tournaments: new Dictionary<TournamentId, StageTournamentFacts>
            {
                [tournamentId] = StageTournamentFactsFactory.Create(
                    tournamentId: tournamentId,
                    seasonId: seasonId,
                    startDate: StageSquadFactFactory.ValidLocalDate),
            });

        return series.Stages.Single();
    }
}