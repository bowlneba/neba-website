namespace Neba.Website.Server.Series;

internal sealed record SeriesStageViewModel
{
    public required string Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public int? AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }
    public required string TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
    public required int TeamSize { get; init; }
}
