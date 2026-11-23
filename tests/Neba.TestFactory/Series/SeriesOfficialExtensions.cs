using Neba.Api.Features.Series.Domain;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.TestFactory.Series;

public static class SeriesOfficialExtensions
{
    /// <summary>
    /// Marks Stages Official in order until one cannot be made Official (not computed, stale, an earlier Stage not Official,
    /// or the last Qualifying Stage before the Backfill). Stands in for a person reviewing each Stage.
    /// </summary>
    public static AdvanceResult MakeReadyStagesOfficial(this SeriesAggregate series, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(series);

        var official = new List<SeriesStageId>();

        foreach (var stage in series.Stages.OrderBy(s => s.Order))
        {
            if (stage.Status == StageStatus.Official)
            {
                continue;
            }

            var result = series.MakeOfficial(stage.Id, series.Version, now);
            if (result.IsError)
            {
                break;
            }

            official.Add(stage.Id);
        }

        return official.Count == 0
            ? AdvanceResult.None
            : new AdvanceResult { Changed = true, StatusChanged = true, OfficialStageIds = official, Version = series.Version };
    }
}