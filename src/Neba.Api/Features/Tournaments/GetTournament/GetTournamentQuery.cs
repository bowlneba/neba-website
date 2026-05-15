using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.GetTournament;

internal sealed record GetTournamentQuery
    : ICachedQuery<ErrorOr<TournamentDetailDto>>
{
    public required TournamentId Id { get; init; }

    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.TournamentDetail(Id);

    public TimeSpan Expiry
        => TimeSpan.FromDays(5);
}