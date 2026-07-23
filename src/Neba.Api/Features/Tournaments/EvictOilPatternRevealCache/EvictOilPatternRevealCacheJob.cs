using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.EvictOilPatternRevealCache;

internal sealed record EvictOilPatternRevealCacheJob
    : IBackgroundJob
{
    public required TournamentId TournamentId { get; init; }

    public required SeasonId SeasonId { get; init; }

    public string JobName
        => $"Evict Oil Pattern Reveal Cache: {TournamentId}";
}
