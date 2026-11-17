namespace Neba.Api.Contracts.Series.GetSeries;

/// <summary>Series stage response.</summary>
public sealed record SeriesStageResponse
{
    /// <summary>The id.</summary>
    public required string Id { get; init; }
    /// <summary>The order.</summary>
    public required int Order { get; init; }
    /// <summary>The kind.</summary>
    public required string Kind { get; init; }
    /// <summary>The status.</summary>
    public required string Status { get; init; }
    /// <summary>The advancing count.</summary>
    public int? AdvancingCount { get; init; }
    /// <summary>The squad date.</summary>
    public DateOnly? SquadDate { get; init; }
    /// <summary>The squad start time.</summary>
    public TimeOnly? SquadStartTime { get; init; }
    /// <summary>The tournament id.</summary>
    public required string TournamentId { get; init; }
    /// <summary>The tournament name.</summary>
    public required string TournamentName { get; init; }
    /// <summary>The tournament start date.</summary>
    public required DateOnly TournamentStartDate { get; init; }
    /// <summary>The tournament end date.</summary>
    public required DateOnly TournamentEndDate { get; init; }
    /// <summary>The tournament type.</summary>
    public required string TournamentType { get; init; }

    /// <summary>Bowlers per entry. Greater than 1 means AdvancingCount counts teams (Qualifying).</summary>
    public required int TeamSize { get; init; }
}
