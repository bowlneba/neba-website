using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.AdvanceOnSquadScoresReplaced;

/// <summary>
/// Advances the Series that holds the tournament after a squad's scores were replaced, but only when a Survivor Stage
/// resolved to that squad. Any other squad does nothing.
/// </summary>
internal sealed class AdvanceOnSquadScoresReplaced(SeriesAdvancement advancement)
    : IDomainEventJob<SquadScoresReplaced>
{
    public Task HandleAsync(SquadScoresReplaced domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return advancement.RunForTournamentAsync(
            domainEvent.TournamentId,
            (series, facts, now) => series.HasStageResolvedTo(domainEvent.SquadId)
                ? series.Advance(domainEvent.TournamentId, facts, now)
                : AdvanceResult.None,
            ct);
    }
}