namespace Neba.Api.Contracts.Series.ListSeries;

/// <summary>Series summary response.</summary>
public sealed record SeriesSummaryResponse
{
    /// <summary>The id.</summary>
    public required string Id { get; init; }
    /// <summary>The name.</summary>
    public required string Name { get; init; }
    /// <summary>The season id.</summary>
    public required string SeasonId { get; init; }
    /// <summary>The season description.</summary>
    public required string SeasonDescription { get; init; }
    /// <summary>The season start date.</summary>
    public required DateOnly SeasonStartDate { get; init; }
    /// <summary>The season end date.</summary>
    public required DateOnly SeasonEndDate { get; init; }
    /// <summary>The qualifying stage count.</summary>
    public required int QualifyingStageCount { get; init; }
    /// <summary>The survivor stage count.</summary>
    public required int SurvivorStageCount { get; init; }
    /// <summary>The placement stage count.</summary>
    public required int PlacementStageCount { get; init; }
    /// <summary>The is locked.</summary>
    public required bool IsLocked { get; init; }
    /// <summary>The logo url.</summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>The tournament id of every Stage, in Stage order.</summary>
    public required IReadOnlyCollection<string> StageTournamentIds { get; init; }

    /// <summary>The first Stage whose tournament hasn't ended; null once every Stage's tournament is over.</summary>
    public SeriesNextStageResponse? NextStage { get; init; }
}