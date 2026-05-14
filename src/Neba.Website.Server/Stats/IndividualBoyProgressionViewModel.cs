namespace Neba.Website.Server.Stats;

/// <summary>
/// BOY race progression data for a single bowler in a single race category on the individual stats page.
/// </summary>
public sealed record IndividualBoyProgressionViewModel
{
    /// <summary>The display label for this race (e.g. "Bowler of the Year", "Senior").</summary>
    public required string RaceLabel { get; init; }

    /// <summary>The points race series for this bowler.</summary>
    public required PointsRaceSeriesViewModel BowlerSeries { get; init; }

    /// <summary>
    /// The current race leader's series, or <c>null</c> when this bowler is the leader.
    /// When non-null, the chart renders both series so the bowler can see how they compare.
    /// </summary>
    public required PointsRaceSeriesViewModel? LeaderSeries { get; init; }
}