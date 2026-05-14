using Neba.Domain.Bowlers;

namespace Neba.Api.Features.Awards.ListHighBlockAwards;

internal sealed record HighBlockAwardDto
{
    public required string Season { get; init; }

    public required Name BowlerName { get; init; }

    public required int Score { get; init; }
}