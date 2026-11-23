namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>A bowler's position in the Placement Round.</summary>
public sealed record SeriesPositionResponse
{
    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The position.</summary>
    public required int Position { get; init; }

    /// <summary>True when the bowler bowled.</summary>
    public required bool Bowled { get; init; }

    /// <summary>True when the bowler is a finalist. Null when not known.</summary>
    public bool? IsFinalist { get; init; }

    /// <summary>The place the bowler finished in the Placement Stage's tournament. Null when unplaced.</summary>
    public int? TournamentPlace { get; init; }
}