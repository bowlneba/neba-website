namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>
/// A side cut section containing all results for one named side cut.
/// </summary>
public sealed record SideCutGroupViewModel
{
    /// <summary>
    /// Name of the side cut.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// CSS hex color for the side cut indicator; null if not set.
    /// </summary>
    public string? Indicator { get; init; }

    /// <summary>
    /// Results belonging to this side cut.
    /// </summary>
    public required IReadOnlyCollection<TournamentResultViewModel> Results { get; init; }
}