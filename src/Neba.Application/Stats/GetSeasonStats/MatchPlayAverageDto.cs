using Neba.Domain.Bowlers;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// A leaderboard entry for the Match Play Average leaderboard, representing a bowler's average pins
/// per match play game during the season. Ordered descending by match play average. Bowlers with zero
/// match play games are excluded. Derived fields are pre-computed.
/// </summary>
public sealed record MatchPlayAverageDto
{
    /// <summary>The unique identifier of the bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }

    /// <summary>Pre-computed average pins per match play game during the season.</summary>
    public required decimal MatchPlayAverage { get; init; }

    /// <summary>Total match play games bowled during the season.</summary>
    public required int Games { get; init; }

    /// <summary>Total match play victories during the season.</summary>
    public required int Wins { get; init; }

    /// <summary>Total match play defeats during the season.</summary>
    public required int Losses { get; init; }

    /// <summary>Pre-computed match play win percentage.</summary>
    public required decimal WinPercentage { get; init; }

    /// <summary>Total prize money earned during the season.</summary>
    public required decimal Winnings { get; init; }
}