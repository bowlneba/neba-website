using Neba.Api.Caching;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

internal sealed record ListTournamentsInSeasonQuery
    : ICachedQuery<IReadOnlyCollection<SeasonTournamentDto>>
{
    public required SeasonId SeasonId { get; init; }

    public required bool CallerIsAuthenticated { get; init; }

    public required bool CallerHasTournamentManagementPermission { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.ListForSeason(SeasonId, CallerIsAuthenticated, CallerHasTournamentManagementPermission);

    public TimeSpan Expiry
        => TimeSpan.FromDays(14);
}