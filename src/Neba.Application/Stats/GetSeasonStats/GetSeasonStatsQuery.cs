using ErrorOr;

using Neba.Application.Messaging;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// Query object representing a request to retrieve the season's statistics, including individual bowler statistics, the Bowler of the Year
/// </summary>
public sealed record GetSeasonStatsQuery
    : IQuery<ErrorOr<SeasonStatsDto>>
{
    /// <summary>
    /// Gets the year of the season for which to retrieve statistics. This property serves as a filter criterion for the query, allowing the requester to specify the particular season they are interested in when retrieving the season's statistics. If this property is null, it may indicate that statistics for the most current season with stats available.
    /// </summary>
    public required int? SeasonYear { get; init; }
}