namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// Oil pattern details included in a tournament detail response, including which rounds it is used in.
/// </summary>
public sealed record TournamentDetailOilPatternResponse
{
    /// <summary>
    /// Name of the pattern (e.g., "Kegel Broadway").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Length of the pattern in feet.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Tournament rounds that use this pattern (e.g., "Qualifying", "Finals").
    /// </summary>
    public IReadOnlyCollection<string> Rounds { get; init; } = [];

    /// <summary>
    /// Kegel pattern library ID; null when the pattern is not in the Kegel library.
    /// </summary>
    public Guid? KegelId { get; init; }
}