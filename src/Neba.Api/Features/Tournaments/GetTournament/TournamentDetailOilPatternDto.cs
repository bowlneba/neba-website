namespace Neba.Api.Features.Tournaments.GetTournament;

/// <summary>
/// Details about the oil pattern used for a specific tournament round, including the name of the pattern and which rounds it applies to.
/// </summary>
public sealed record TournamentDetailOilPatternDto
{
    /// <summary>
    /// Gets the name associated with the current instance.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the length value
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// The tournament rounds that use this oil pattern (e.g., "Qualifying", "Round 1", "Finals"). This allows for tournaments that use different patterns in different rounds.
    /// </summary>
    public required IReadOnlyCollection<string> TournamentRounds { get; init; }
}
