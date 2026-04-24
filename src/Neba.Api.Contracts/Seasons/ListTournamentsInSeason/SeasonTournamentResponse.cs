namespace Neba.Api.Contracts.Seasons.ListTournamentsInSeason;

/// <summary>
/// Summary details for a tournament returned by the list tournaments in season endpoint.
/// </summary>
public sealed record SeasonTournamentResponse
{
    /// <summary>
    /// The unique tournament identifier as a ULID string.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Publicly displayed tournament name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Date the first qualifying squad is held.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Date the final round concludes; equals StartDate for single-day events.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Format category of the tournament (e.g., "Singles", "Doubles").
    /// </summary>
    public required string TournamentType { get; init; }

    /// <summary>
    /// Per-bowler entry fee in USD; null if not set.
    /// </summary>
    public decimal? EntryFee { get; init; }

    /// <summary>
    /// External URL for online registration; null if not available.
    /// </summary>
    public Uri? RegistrationUrl { get; init; }

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
    /// URL to the tournament logo image; null when unavailable.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Display names of the winning bowler(s); empty for pending or upcoming events.
    /// </summary>
    public IReadOnlyCollection<string> Winners { get; init; } = [];

    /// <summary>
    /// Host bowling center; null until confirmed.
    /// </summary>
    public TournamentBowlingCenterResponse? BowlingCenter { get; init; }

    /// <summary>
    /// Sponsors associated with this tournament.
    /// </summary>
    public IReadOnlyCollection<TournamentSponsorResponse> Sponsors { get; init; } = [];

    /// <summary>
    /// Oil patterns used in this tournament, including which rounds each applies to.
    /// </summary>
    public IReadOnlyCollection<TournamentOilPatternResponse> OilPatterns { get; init; } = [];
}
