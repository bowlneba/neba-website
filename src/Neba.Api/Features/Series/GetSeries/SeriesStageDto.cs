using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed record SeriesStageDto
{
    public required SeriesStageId Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public required string Status { get; init; }
    public int? AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
    public required string TournamentType { get; init; }
    public required int TeamSize { get; init; }
}
