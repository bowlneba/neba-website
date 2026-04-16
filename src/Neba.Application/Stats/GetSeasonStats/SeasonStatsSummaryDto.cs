using Neba.Domain.Bowlers;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// Data transfer object representing a summary of the season's statistics, including total entries and total prize money. This DTO is used to transfer aggregated season statistics data from the application layer to the presentation layer, providing a concise overview of the season's performance metrics.
/// </summary>
public sealed record SeasonStatsSummaryDto
{
    /// <summary>
    /// Gets the total number of entries for the season, representing the total count of participants or entries in the season's events. This metric provides insight into the level of participation and engagement throughout the season.
    /// </summary>
    public required int TotalEntries { get; init; }

    /// <summary>
    /// Gets the total prize money for the season, representing the cumulative amount of prize money awarded across all events in the season. This metric serves as an indicator of the financial stakes and rewards associated with the season's competitions.
    /// </summary>
    public required decimal TotalPrizeMoney { get; init; }

    /// <summary>
    /// Gets the highest game score achieved during the season, representing the maximum score recorded in a single game throughout the season. This metric highlights the peak performance level attained by any participant during the season's events.
    /// </summary>
    public required int HighGame { get; init; }

    /// <summary>
    /// Gets a dictionary mapping bowler IDs to their names for those who achieved the highest game score during the season. This provides context and recognition for the bowlers who reached the highest game score, allowing for easy identification of the top performers in this category.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighGameBowlers { get; init; }

    /// <summary>
    /// Gets the highest block score achieved during the season, representing the maximum cumulative score recorded in a block of games throughout the season. This metric highlights the consistency and endurance of participants across multiple games, showcasing those who excelled in maintaining high performance over a series of games.
    /// </summary>
    public required int HighBlock { get; init; }

    /// <summary>
    /// Gets a dictionary mapping bowler IDs to their names for those who achieved the highest block score during the season. This provides context and recognition for the bowlers who reached the highest block score, allowing for easy identification of the top performers in this category and acknowledging their sustained excellence across multiple games.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighBlockBowlers { get; init; }

    /// <summary>
    /// Gets the highest average score achieved during the season, representing the maximum average score recorded by any participant across all games played during the season. This metric highlights the overall performance level and consistency of participants throughout the season, showcasing those who maintained a high average score across their games.
    /// </summary>
    public required decimal HighAverage { get; init; }

    /// <summary>
    /// Gets a dictionary mapping bowler IDs to their names for those who achieved the highest average score during the season. This provides context and recognition for the bowlers who reached the highest average score, allowing for easy identification of the top performers in this category and acknowledging their consistent excellence across all games played during the season.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighAverageBowlers { get; init; }

    /// <summary>
    /// Gets the highest series score achieved during the season, representing the maximum cumulative score recorded in a series of games throughout the season. This metric highlights the sustained performance and skill of participants across multiple games, showcasing those who excelled in maintaining high scores over an extended series of games.
    /// </summary>
    public required decimal HighestMatchPlayWinPercentage { get; init; }

    /// <summary>
    /// Gets a dictionary mapping bowler IDs to their names for those who achieved the highest match play win percentage during the season. This provides context and recognition for the bowlers who reached the highest match play win percentage, allowing for easy identification of the top performers in this category and acknowledging their strategic excellence and consistency in match play scenarios throughout the season.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighestMatchPlayWinPercentageBowlers { get; init; }

    /// <summary>
    /// Gets the most finals appearances achieved during the season, representing the maximum number of times a participant reached the finals in events throughout the season. This metric highlights the competitive consistency and high-level performance of participants, showcasing those who frequently advanced to the final stages of competitions during the season.
    /// </summary>
    public required int MostFinals { get; init; }

    /// <summary>
    /// Gets a dictionary mapping bowler IDs to their names for those who achieved the most finals appearances during the season. This provides context and recognition for the bowlers who reached the most finals appearances, allowing for easy identification of the top performers in this category and acknowledging their consistent ability to advance to the final stages of competitions throughout the season.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> MostFinalsBowlers { get; init; }
}