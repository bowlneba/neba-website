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
    /// The bowler's family or surname. Exposed separately from <see cref="BowlerName"/> so clients can sort
    /// or group bowlers by last name (e.g. breaking ties in a title-count leaderboard) without having to
    /// parse the formatted display name.
    /// </summary>
    public required string BowlerLastName { get; init; }

    /// <summary>
    /// The bowler's given first name. Exposed separately from <see cref="BowlerName"/> for the same reason
    /// as <see cref="BowlerLastName"/> — sorting by last name, then first name.
    /// </summary>
    public required string BowlerFirstName { get; init; }

    /// <summary>
    /// Indicates whether the bowler is in the Hall of Fame.
    /// </summary>
    public required bool HallOfFame { get; init; }
}