namespace Neba.Api.Features.Tournaments.CheckTeamEligibility;

internal sealed record CheckTeamEligibilityRequest
{
    public required string Id { get; init; }

    public string[] BowlerIds { get; init; } = [];

    public int UnknownBowlerCount { get; init; }
}