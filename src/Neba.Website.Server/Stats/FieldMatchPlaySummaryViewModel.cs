namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for the field match play summary statistics.
/// </summary>
public sealed record FieldMatchPlaySummaryViewModel
{
    /// <summary>
    /// Gets the highest win percentage among bowlers who have played at least 10 matches.
    /// </summary>
    public required decimal HighestWinPercentage { get; init; }

    /// <summary>
    /// Gets the bowlers with the highest win percentage, along with their names.
    /// </summary>
    public required IReadOnlyDictionary<string, string> HighestWinPercentageBowlers { get; init; }

    /// <summary>
    /// Gets the most finals appearances among bowlers who have played at least 10 matches.
    /// </summary>
    public required int MostFinals { get; init; }

    /// <summary>
    /// Gets the bowlers with the most finals appearances, along with their names.
    /// </summary>
    public required IReadOnlyDictionary<string, string> MostFinalsBowlers { get; init; }
}