namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>A NEBA title, with the dates of the tournament it was won in.</summary>
internal readonly record struct NebaTitle(DateOnly StartDate, DateOnly EndDate);
