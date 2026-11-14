namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>The tournament a team wants to enter.</summary>
internal readonly record struct TeamEligibilityContext(TournamentType TournamentType, DateOnly StartDate, DateOnly EndDate);
