using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Tournaments.ListChampions;

/// <summary>
/// Represents a tournament champion in the list of champions response. This DTO is used to transfer data from the domain model to the API response model, ensuring that only the necessary information is exposed to the client.
/// </summary>
public sealed record TournamentChampionsDto
{
    /// <summary>
    /// The unique identifier of the tournament. This is used to link to the tournament's details and other related data.
    /// </summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>
    /// The name of the tournament. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required string TournamentName { get; init; }

    /// <summary>
    /// The date of the tournament. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required DateOnly TournamentDate { get; init; }

    /// <summary>
    /// The type of the tournament. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required string TournamentType { get; init; }

    /// <summary>
    /// The champion of the tournament. This includes the bowler's name, whether they are in the Hall of Fame, and other relevant details. This is used for display purposes and to provide context about the champion's achievement.
    /// </summary>
    public required IReadOnlyCollection<ChampionDto> Champions { get; init; }

}