using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Tournament data a Series needs to check its Stages. Queried by the handler.</summary>
public sealed record StageTournamentFacts
{
    /// <summary>The tournament.</summary>
    public required TournamentId TournamentId { get; init; }
    /// <summary>The tournament name, used in error messages.</summary>
    public required string TournamentName { get; init; }
    /// <summary>The tournament's season.</summary>
    public required SeasonId SeasonId { get; init; }
    /// <summary>First day of the tournament.</summary>
    public required DateOnly StartDate { get; init; }
    /// <summary>Last day of the tournament.</summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>Name of another Series that already has this tournament as a Stage, if any.</summary>
    public string? OtherSeriesName { get; init; }
}