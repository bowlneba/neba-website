using Neba.Api.Caching;
using Neba.Api.Messaging;
using Neba.Domain.Seasons;

namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

internal sealed record ListTournamentsInSeasonQuery
    : ICachedQuery<IReadOnlyCollection<SeasonTournamentDto>>
{
    public required SeasonId SeasonId { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.ListForSeason(SeasonId);

    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}