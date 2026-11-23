using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.AdvanceOnResultsRecorded;

/// <summary>
/// Advances the Series that holds the tournament once its results were written. A tournament in no Series does nothing.
/// A rerun recomputes a Computed Stage to the same result, and an Official Stage only goes Stale when a dry compute differs.
/// </summary>
internal sealed class AdvanceOnResultsRecorded(SeriesAdvancement advancement)
    : IDomainEventJob<TournamentResultsRecorded>
{
    public Task HandleAsync(TournamentResultsRecorded domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return advancement.RunForTournamentAsync(
            domainEvent.TournamentId,
            (series, facts, now) => series.Advance(domainEvent.TournamentId, facts, now),
            ct);
    }
}