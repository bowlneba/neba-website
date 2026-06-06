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
    /// Gets the Kegel pattern ID if this pattern is associated with a Kegel pattern in the Kegel API; null if there is no association. This allows clients to link to the corresponding pattern details in the Kegel system when available, while still supporting patterns that are not in Kegel. Note that not all patterns will have a Kegel association, as some tournaments may use custom or proprietary patterns that are not listed in the Kegel database.
    /// </summary>
    public Guid? KegelId { get; init; }

    /// <summary>
    /// The tournament rounds that use this oil pattern (e.g., "Qualifying", "Round 1", "Finals"). This allows for tournaments that use different patterns in different rounds.
    /// </summary>
    public required IReadOnlyCollection<string> TournamentRounds { get; init; }
}