namespace Neba.Api.Features.Series.GetSeries;

internal sealed record SeriesFinaleDto
{
    public required int FinalistCount { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public string? BowlingCenterId { get; init; }
    public string? BowlingCenterName { get; init; }
    public string? BowlingCenterCity { get; init; }
    public string? BowlingCenterState { get; init; }
}
