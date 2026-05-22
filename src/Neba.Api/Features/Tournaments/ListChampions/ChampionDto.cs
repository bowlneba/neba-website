using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.ListChampions;

/// <summary>
/// Represents a tournament champion in the list of champions response. This DTO is used to transfer data from the domain model to the API response model, ensuring that only the necessary information is exposed to the client.
/// </summary>
public sealed record ChampionDto
{
    /// <summary>
    /// The unique identifier of the bowler. This is used to link to the bowler's profile and other related data.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// Indicates whether the bowler is in the Hall of Fame. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required bool HallOfFame { get; init; }
}