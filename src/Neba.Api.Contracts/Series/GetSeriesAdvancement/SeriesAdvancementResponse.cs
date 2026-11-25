namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>A Series' advancement: each tournament's status and output.</summary>
public sealed record SeriesAdvancementResponse
{
    /// <summary>Concurrency token. Writes send the value from the last GET.</summary>
    public required int Version { get; init; }

    /// <summary>The date the Bowler of the Year point list ran as of. Null until it ran.</summary>
    public DateOnly? BackfillAsOf { get; init; }

    /// <summary>Target field size: computed Qualifying slots, else advancing count times team size.</summary>
    public required int TargetField { get; init; }

    /// <summary>The tournaments, in order.</summary>
    public required IReadOnlyList<StageAdvancementResponse> Stages { get; init; }

    /// <summary>The Placement Round's positions. Empty until it computes.</summary>
    public required IReadOnlyList<SeriesPositionResponse> Positions { get; init; }
}