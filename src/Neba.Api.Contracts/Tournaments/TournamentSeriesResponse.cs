namespace Neba.Api.Contracts.Tournaments;

/// <summary>The Series a tournament is a Stage of.</summary>
public sealed record TournamentSeriesResponse
{
    /// <summary>The Series ID.</summary>
    public required string SeriesId { get; init; }

    /// <summary>The Series name.</summary>
    public required string Name { get; init; }

    /// <summary>The Series' own logo, or null (the page shows initials).</summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>Qualifying, Survivor or Placement (SmartEnum name).</summary>
    public required string Kind { get; init; }

    /// <summary>Top N (Qualifying) or bowlers advancing (Survivor). Null on a Placement Stage.</summary>
    public int? AdvancingCount { get; init; }

    /// <summary>Survivor Stages only: the squad's date.</summary>
    public DateOnly? SquadDate { get; init; }

    /// <summary>Survivor Stages only: the squad's start time.</summary>
    public TimeOnly? SquadStartTime { get; init; }

    /// <summary>Placement Stages only, and only when the Series has a Finale: how many finalists go to it.</summary>
    public int? FinaleCount { get; init; }

    /// <summary>Placement Stages only, when the Series has a Finale: the Finale's date.</summary>
    public DateOnly? FinaleDate { get; init; }

    /// <summary>Placement Stages only, when the Series has a Finale: the Finale's start time.</summary>
    public TimeOnly? FinaleStartTime { get; init; }
}