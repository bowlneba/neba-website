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
    /// Volume of oil applied in milliliters.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// Ratio of inner boards to left outside boards.
    /// </summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>
    /// Ratio of inner boards to right outside boards.
    /// </summary>
    public required decimal RightRatio { get; init; }

    /// <summary>
    /// Optional GUID identifying this pattern in the Kegel public pattern library.
    /// </summary>
    public Guid? KegelId { get; init; }

    /// <summary>
    /// Tournament rounds that use this pattern (e.g., "Qualifying", "Finals").
    /// </summary>
    public IReadOnlyCollection<string> Rounds { get; init; } = [];
}