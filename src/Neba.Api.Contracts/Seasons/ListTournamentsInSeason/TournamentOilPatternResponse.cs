namespace Neba.Api.Contracts.Seasons.ListTournamentsInSeason;

/// <summary>
/// Oil pattern details for a specific tournament, including the rounds in which it is used.
/// </summary>
public sealed record TournamentOilPatternResponse
{
    /// <summary>
    /// Name of the oil pattern (e.g., "Kegel Broadway").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Length of the oil pattern in feet.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Volume of oil applied in milliliters.
    /// </summary>
    public required decimal Volume { get; init; }

    /// <summary>
    /// Ratio of inner-lane oil to left outside boards.
    /// </summary>
    public required decimal LeftRatio { get; init; }

    /// <summary>
    /// Ratio of inner-lane oil to right outside boards.
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
