using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the Match Play Record leaderboard, representing a bowler's match play
/// win/loss record during the season. Ordered descending by win percentage. Bowlers with no match
/// play record are excluded. Derived fields are pre-computed.
/// </summary>
public sealed record MatchPlayRecordDto
{
    /// <summary>
    /// The unique identifier of the bowler.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The bowler's display name.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// Total match play victories during the season.
    /// </summary>
    public required int Wins { get; init; }

    /// <summary>
    /// Total match play defeats during the season.
    /// </summary>
    public required int Losses { get; init; }

    /// <summary>
    /// Pre-computed match play win percentage.
    /// </summary>
    public required decimal WinPercentage { get; init; }

    /// <summary>
    /// The number of Finals appearances during the season.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// Pre-computed average pins per match play game during the season.
    /// </summary>
    public required decimal MatchPlayAverage { get; init; }

    /// <summary>
    /// Total prize money earned during the season.
    /// </summary>
    public required decimal Winnings { get; init; }
}