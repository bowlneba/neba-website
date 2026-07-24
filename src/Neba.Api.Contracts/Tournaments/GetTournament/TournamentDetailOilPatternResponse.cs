namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// Oil pattern details included in a tournament detail response, including which rounds it is used in.
/// </summary>
public sealed record TournamentDetailOilPatternResponse
{
    /// <summary>
    /// The unique identifier for the oil pattern, as a ULID string.
    /// </summary>
    public required string OilPatternId { get; init; }

    /// <summary>
    /// Name of the pattern (e.g., "Kegel Broadway").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Length of the pattern in feet.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// The oil volume applied, in milliliters.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// The forward (head) to reverse (tail) oil ratio on the pattern's left side.
    /// </summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>
    /// The forward (head) to reverse (tail) oil ratio on the pattern's right side.
    /// </summary>
    public required decimal RightRatio { get; init; }

    /// <summary>
    /// Tournament rounds that use this pattern (e.g., "Qualifying", "Finals").
    /// </summary>
    public IReadOnlyCollection<string> Rounds { get; init; } = [];

    /// <summary>
    /// Kegel pattern library ID; null when the pattern is not in the Kegel library.
    /// </summary>
    public Guid? KegelId { get; init; }
}