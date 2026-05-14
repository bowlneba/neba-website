using Neba.Api.Features.Seasons.ListSeasons;
using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.Api.Features.Tournaments.ListTournamentsInSeason;

/// <summary>
/// Summary of a tournament returned by list queries.
/// </summary>
public sealed record SeasonTournamentDto
{
    /// <summary>
    /// Unique tournament identifier.
    /// </summary>
    public required TournamentId Id { get; init; }

    /// <summary>
    /// Publicly displayed tournament name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Season this tournament belongs to.
    /// </summary>
    public required SeasonDto Season { get; init; }

    /// <summary>
    /// Date the first qualifying squad is held.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Date the final round concludes; equals StartDate for single-day events.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Whether the tournament is eligible for season-long stats and awards (typically false for non-standard events like match play or special formats).
    /// </summary>
    public required bool StatsEligible { get; init; }

    /// <summary>
    /// Format category of the tournament.
    /// </summary>
    public required string TournamentType { get; init; }

    /// <summary>
    /// Per-bowler entry fee in USD; null if not set.
    /// </summary>
    public required decimal? EntryFee { get; init; }

    /// <summary>
    /// External URL for online registration.
    /// </summary>
    public required Uri? RegistrationUrl { get; init; }

    /// <summary>
    /// Host bowling center; null until confirmed.
    /// </summary>
    public required TournamentBowlingCenterDto? BowlingCenter { get; init; }

    /// <summary>
    /// Sponsors associated with this tournament.
    /// </summary>
    public IReadOnlyCollection<TournamentSponsorDto> Sponsors { get; init; } = [];

    /// <summary>
    /// Sponsor-added prize money in USD; null if none.
    /// </summary>
    public decimal? AddedMoney { get; init; }

    /// <summary>
    /// Current reservation count; null until tracking begins.
    /// </summary>
    public int? Reservations { get; init; }

    /// <summary>
    /// Pattern length bucket label; null until set.
    /// </summary>
    public string? PatternLengthCategory { get; init; }

    /// <summary>
    /// Pattern ratio category; null until set.
    /// </summary>
    public string? PatternRatioCategory { get; init; }

    /// <summary>
    /// Oil pattern details; null until published.
    /// </summary>
    public IReadOnlyCollection<TournamentOilPatternDto> OilPatterns { get; init; } = [];

    /// <summary>
    /// URL to the tournament logo image; null when unavailable.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Storage container for the logo; null when unavailable.
    /// </summary>
    public string? LogoContainer { get; init; }

    /// <summary>
    /// Storage path for the logo; null when unavailable.
    /// </summary>
    public string? LogoPath { get; init; }

    /// <summary>
    /// Names of the winning bowler(s); empty for pending/upcoming events.
    /// </summary>
    public IReadOnlyCollection<Name> Winners { get; init; } = [];
}