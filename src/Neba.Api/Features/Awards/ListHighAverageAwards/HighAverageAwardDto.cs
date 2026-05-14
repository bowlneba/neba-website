using Neba.Domain.Bowlers;

namespace Neba.Api.Features.Awards.ListHighAverageAwards;

internal sealed record HighAverageAwardDto
{
    public required string Season { get; init; }

    public required Name BowlerName { get; init; }

    public required decimal Average { get; init; }

    public int? TotalGames { get; init; }

    public int? TournamentsParticipated { get; init; }
}