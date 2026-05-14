using Neba.Domain.Bowlers;

namespace Neba.Api.Features.Awards.ListBowlerOfTheYearAwards;


internal sealed record BowlerOfTheYearAwardDto
{
    public required string Season { get; init; }

    public required Name BowlerName { get; init; }

    public required string Category { get; init; }
}