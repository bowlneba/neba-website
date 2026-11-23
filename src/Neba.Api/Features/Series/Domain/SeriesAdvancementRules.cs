using ErrorOr;

namespace Neba.Api.Features.Series.Domain;

internal static class SeriesAdvancementRules
{
    /// <summary>
    /// Why a Stage cannot be made Official, or null when it can. A Computed Stage that is not stale can be
    /// made Official once every earlier Stage is Official, and, for the last Qualifying Stage, once the
    /// Backfill ran. Shared by the make-official write and the advancement read. An Official Stage is not checked here.
    /// </summary>
    public static Error? MakeOfficialBlocker(
        StageStatus status, bool isStale, bool awaitingBackfill, int? firstUnofficialEarlierOrder)
    {
        if (status != StageStatus.Computed)
        {
            return SeriesAdvancementErrors.OfficialStageNotComputed;
        }

        if (isStale)
        {
            return SeriesAdvancementErrors.OfficialStageStale;
        }

        if (firstUnofficialEarlierOrder is { } order)
        {
            return SeriesAdvancementErrors.OfficialEarlierStageNotOfficial(order);
        }

        return awaitingBackfill ? SeriesAdvancementErrors.OfficialAwaitingBackfill : null;
    }
}