namespace Neba.Api.Contracts.Tournaments.ListChampions;

/// <summary>
/// Represents a tournament champion in the list of champions response.
/// </summary>
public sealed record TournamentChampionResponse
{
    /// <summary>
    /// The unique identifier of the bowler. This is used to link to the bowler's profile and other related data.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Indicates whether the bowler is in the Hall of Fame.
    /// </summary>
    public required bool HallOfFame { get; init; }

    /// <summary>
    /// The unique identifier of the tournament. This is used to link to the tournament's details and other related data.
    /// </summary>
    public required string TournamentId { get; init; }

    /// <summary>
    /// The name of the tournament. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required string TournamentName { get; init; }

    /// <summary>
    /// The date of the tournament. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required string TournamentDate { get; init; }

    /// <summary>
    /// The type of the tournament. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required string TournamentType { get; init; }
}