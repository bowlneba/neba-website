namespace Neba.Application.Tournaments;

/// <summary>
/// Details about the oil pattern used for a specific tournament round, including the name of the pattern and which rounds it applies to.
/// </summary>
public sealed record TournamentOilPatternDto
{
    /// <summary>
    /// The name of the oil pattern used for this tournament round (e.g., "Kegel Broadway", "Brunswick Axiom").
    /// </summary>
    public required OilPatternDto OilPattern { get; init; }

    /// <summary>
    /// The tournament rounds that use this oil pattern (e.g., "Qualifying", "Round 1", "Finals"). This allows for tournaments that use different patterns in different rounds.
    /// </summary>
    public required IReadOnlyCollection<string> TournamentRounds { get; init; }
}