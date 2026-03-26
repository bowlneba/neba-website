namespace Neba.Website.Server.History.Awards;

/// <summary>
/// View model used by the High Block UI to display a single season's award entry.
/// Supports multiple bowlers for tied scores.
/// </summary>
public sealed record HighBlockAwardViewModel
{
    /// <summary>
    /// The season in which the high block score was achieved (e.g., "2025 Season").
    /// </summary>
    public required string Season { get; init; }

    /// <summary>
    /// The full display name(s) of the bowler(s) who achieved the high block score.
    /// Contains more than one entry when multiple bowlers tied with the same score.
    /// </summary>
    public required IReadOnlyCollection<string> Bowlers { get; init; }

    /// <summary>
    /// The total pinfall score across five consecutive qualifying games.
    /// </summary>
    public required int Score { get; init; }
}
