using Neba.Domain.Seasons;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// Represents a season along with its associated statistical information.
/// </summary>
public sealed record SeasonWithStatsDto
{
    /// <summary>
    /// Gets the unique identifier for the season.
    /// </summary>
    public required SeasonId Id { get; init; }

    /// <summary>
    /// Gets the description associated with this instance.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the start date for the associated entity or operation.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets the end date for the period or event represented by this instance.
    /// </summary>
    public required DateOnly EndDate { get; init; }
}