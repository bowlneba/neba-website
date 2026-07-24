using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.ListTournamentTypes;

internal sealed record ListTournamentTypesQuery
    : ICachedQuery<IReadOnlyCollection<TournamentTypeSummaryDto>>
{
    public CacheDescriptor Cache
        => CacheDescriptors.Tournaments.Types;

    public TimeSpan Expiry
        => TimeSpan.FromDays(90);
}