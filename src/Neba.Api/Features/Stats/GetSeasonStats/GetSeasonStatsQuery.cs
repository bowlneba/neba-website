using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Stats.GetSeasonStats;

internal sealed record GetSeasonStatsQuery
    : IQuery<ErrorOr<SeasonStatsDto>>
{
    public required int? SeasonYear { get; init; }
}