using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.AdvanceOnTournamentChanged;

/// <summary>
/// Advances the Series that holds the tournament after the tournament was cancelled or its dates or type changed. A
/// Stage whose tournament can no longer compute goes Stale instead of reaching Official. A tournament in no Series does nothing.
/// </summary>
internal sealed class AdvanceOnTournamentChanged(SeriesAdvancement advancement)
    : IDomainEventJob<TournamentChanged>
{
    public Task HandleAsync(TournamentChanged domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return advancement.RunForTournamentAsync(
            domainEvent.TournamentId,
            (series, facts, now) => series.Advance(domainEvent.TournamentId, facts, now),
            ct);
    }
}
