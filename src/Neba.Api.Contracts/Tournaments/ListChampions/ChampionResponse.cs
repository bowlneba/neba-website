namespace Neba.Api.Contracts.Tournaments.ListChampions;

/// <summary>
/// Represents a champion in the list of tournament champions response. This includes the bowler's name, whether they are in the Hall of Fame, and other relevant details. This is used for display purposes and to provide context about the champion's achievement.
/// </summary>
public sealed record ChampionResponse
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
}