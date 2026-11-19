namespace Neba.Api.Contracts.Series.ListSeries;

/// <summary>Series next stage response.</summary>
public sealed record SeriesNextStageResponse
{
    /// <summary>"Qualifying", "Survivor", or "Placement".</summary>
    public required string Kind { get; init; }
    /// <summary>The tournament id.</summary>
    public required string TournamentId { get; init; }
    /// <summary>The tournament name.</summary>
    public required string TournamentName { get; init; }
    /// <summary>The tournament start date.</summary>
    public required DateOnly TournamentStartDate { get; init; }
    /// <summary>The tournament end date.</summary>
    public required DateOnly TournamentEndDate { get; init; }
}