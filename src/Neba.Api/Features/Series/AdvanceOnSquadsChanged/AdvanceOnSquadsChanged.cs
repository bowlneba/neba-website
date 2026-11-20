using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.AdvanceOnSquadsChanged;

/// <summary>
/// Advances the Series that holds the tournament after a squad was added, changed or removed. Advance resolves the
/// Survivor squad first, so a new squad resolves here and a vanished one reverts the designation and marks the Stage Stale.
/// </summary>
internal sealed class AdvanceOnSquadsChanged(SeriesAdvancement advancement)
    : IDomainEventJob<TournamentSquadsChanged>
{
    public Task HandleAsync(TournamentSquadsChanged domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return advancement.RunForTournamentAsync(
            domainEvent.TournamentId,
            (series, facts, now) => series.Advance(domainEvent.TournamentId, facts, now),
            ct);
    }
}