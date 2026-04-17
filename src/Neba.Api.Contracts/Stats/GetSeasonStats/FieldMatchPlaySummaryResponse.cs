namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>Summary of the best field match play performances during the Season.</summary>
public sealed record FieldMatchPlaySummaryResponse
{
    /// <summary>The highest match play win percentage achieved by any bowler during the Season, expressed as a percentage (0–100).</summary>
    public required decimal HighestWinPercentage { get; init; }

    /// <summary>The bowler(s) who achieved the highest match play win percentage, keyed by ULID string to display name.</summary>
    public required IReadOnlyDictionary<string, string> HighestWinPercentageBowlers { get; init; }

    /// <summary>The highest number of Finals appearances achieved by any bowler during the Season.</summary>
    public required int MostFinals { get; init; }

    /// <summary>The bowler(s) who achieved the most Finals appearances, keyed by ULID string to display name.</summary>
    public required IReadOnlyDictionary<string, string> MostFinalsBowlers { get; init; }
}