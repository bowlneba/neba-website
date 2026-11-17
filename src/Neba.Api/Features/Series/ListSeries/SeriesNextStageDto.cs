using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.ListSeries;

internal sealed record SeriesNextStageDto
{
    public required string Kind { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
}
