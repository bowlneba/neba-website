namespace Neba.Website.Server.Series;

internal sealed record SeriesSummaryViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int SeasonYear { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required string SeasonDateRange { get; init; }
    public required int QualifyingStageCount { get; init; }
    public required int SurvivorStageCount { get; init; }
    public required int PlacementStageCount { get; init; }
    public required bool IsLocked { get; init; }
    public Uri? LogoUrl { get; init; }
    public SeriesNextStageViewModel? NextStage { get; init; }
}

internal sealed record SeriesNextStageViewModel
{
    public required string Kind { get; init; }
    public required string TournamentName { get; init; }
    public required string Dates { get; init; }
}