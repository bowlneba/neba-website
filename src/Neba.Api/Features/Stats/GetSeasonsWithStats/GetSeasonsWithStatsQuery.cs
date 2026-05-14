using Neba.Api.Messaging;
using Neba.Domain.Seasons;

namespace Neba.Api.Features.Stats.GetSeasonsWithStats;

internal sealed record GetSeasonsWithStatsQuery
    : IQuery<IReadOnlyCollection<SeasonWithStatsDto>>;
