namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>Best individual performances achieved during the Season.</summary>
public sealed record SeasonBestsResponse
{
    /// <summary>The highest single qualifying game bowled by any bowler during the Season.</summary>
    public required int HighGame { get; init; }

    /// <summary>Display names of the bowler(s) who achieved the high game.</summary>
    public required IReadOnlyCollection<string> HighGameBowlers { get; init; }

    /// <summary>The highest 5-game qualifying block score achieved by any bowler during the Season.</summary>
    public required int HighBlock { get; init; }

    /// <summary>Display names of the bowler(s) who achieved the high block.</summary>
    public required IReadOnlyCollection<string> HighBlockBowlers { get; init; }

    /// <summary>The highest overall average achieved by any bowler during the Season.</summary>
    public required decimal HighAverage { get; init; }

    /// <summary>Display names of the bowler(s) who achieved the high average.</summary>
    public required IReadOnlyCollection<string> HighAverageBowlers { get; init; }
}
