namespace Neba.Api.Features.Series.Domain;

internal static class SeriesAdvancementRules
{
    /// <summary>
    /// When a Computed Qualifying or Survivor Stage will become Official (the grace period after the output last changed), or null
    /// when it will not. Shared by the make-official job and the advancement read. It ignores whether earlier Stages are
    /// official, which delays but never advances it.
    /// </summary>
    public static DateTimeOffset? OfficialDueAt(
        StageKind kind, StageStatus status, bool isStale, DateTimeOffset? computedAt, bool awaitingBackfill, TimeSpan grace)
        => kind != StageKind.Placement && status == StageStatus.Computed && !isStale && !awaitingBackfill && computedAt is { } at
            ? at + grace
            : null;
}