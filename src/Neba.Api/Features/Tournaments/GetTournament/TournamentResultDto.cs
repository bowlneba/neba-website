using System.Drawing;

using Neba.Domain.Bowlers;

namespace Neba.Api.Features.Tournaments.GetTournament;

/// <summary>
/// Data transfer object representing the result of a bowler in a tournament, including their name, placement, prize money, points earned, and any side cut information.
/// </summary>
public sealed record TournamentResultDto
{
    /// <summary>
    /// The name of the bowler associated with this tournament result. This property is required and must be provided when creating an instance of TournamentResultDto.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// The placement of the bowler in the tournament. This property is nullable to accommodate cases where a bowler may not have a placement (e.g., if they were disqualified or did not finish). If a placement is provided, it should be a positive integer representing the bowler's rank in the tournament.
    /// </summary>
    public int? Place { get; init; }

    /// <summary>
    /// The amount of prize money awarded to the bowler for their performance in the tournament. This property is a decimal value that can be zero or positive, depending on the tournament's prize structure and the bowler's placement. It is initialized to zero by default, but can be set to a specific amount when creating an instance of TournamentResultDto.
    /// </summary>
    public decimal PrizeMoney { get; init; }

    /// <summary>
    /// The number of points earned by the bowler for their performance in the tournament. This property is an integer value that can be zero or positive, depending on the tournament's scoring system and the bowler's placement. It is initialized to zero by default, but can be set to a specific number of points when creating an instance of TournamentResultDto.
    /// </summary>
    public int Points { get; init; }

    /// <summary>
    /// The name of the side cut that the bowler qualified for, if applicable. This property is nullable to accommodate cases where a bowler may not have qualified for any side cuts. If a side cut name is provided, it should be a non-empty string representing the name of the side cut group that the bowler qualified for based on their performance in the tournament.
    /// </summary>
    public string? SideCutName { get; init; }

    /// <summary>
    /// The color associated with the side cut that the bowler qualified for, if applicable. This property is nullable to accommodate cases where a bowler may not have qualified for any side cuts. If a color is provided, it should be a valid Color value representing the color assigned to the side cut group that the bowler qualified for based on their performance in the tournament.
    /// </summary>
    public Color? SideCutIndicator { get; init; }
}