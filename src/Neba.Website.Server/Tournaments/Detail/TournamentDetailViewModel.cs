namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>
/// Full detail view model for a single tournament page.
/// </summary>
public sealed record TournamentDetailViewModel
{
    /// <summary>
    /// Unique tournament identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Publicly displayed tournament name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable season description, e.g. "2025-2026 Season".
    /// </summary>
    public required string SeasonDescription { get; init; }

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
    /// Whether the tournament counts toward season-long stats and awards.
    /// </summary>
    public required bool StatsEligible { get; init; }

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
    /// Total entries in the tournament; null when unknown.
    /// </summary>
    public int? EntryCount { get; init; }

    /// <summary>
    /// Pattern length bucket label; null until set.
    /// </summary>
    public string? PatternLengthCategory { get; init; }

    /// <summary>
    /// URL to the tournament logo image; null when unavailable.
    /// </summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>
    /// Name of the host bowling center.
    /// </summary>
    public string? BowlingCenterName { get; init; }

    /// <summary>
    /// City of the host bowling center.
    /// </summary>
    public string? BowlingCenterCity { get; init; }

    /// <summary>
    /// State of the host bowling center (two-letter code).
    /// </summary>
    public string? BowlingCenterState { get; init; }

    /// <summary>
    /// Sponsors associated with this tournament.
    /// </summary>
    public IReadOnlyCollection<TournamentDetailSponsorViewModel> Sponsors { get; init; } = [];

    /// <summary>
    /// Oil patterns used in this tournament.
    /// </summary>
    public IReadOnlyCollection<TournamentDetailOilPatternViewModel> OilPatterns { get; init; } = [];

    /// <summary>
    /// Display names of the winning bowler(s); empty for pending or upcoming events.
    /// </summary>
    public IReadOnlyCollection<string> Winners { get; init; } = [];

    /// <summary>
    /// Per-bowler results; empty for upcoming or data-unavailable tournaments.
    /// </summary>
    public IReadOnlyCollection<TournamentResultViewModel> Results { get; init; } = [];

    /// <summary>
    /// True when results are available.
    /// </summary>
    public bool HasResults => Results.Count > 0;

    /// <summary>
    /// True when a registration URL is available.
    /// </summary>
    public bool HasRegistrationUrl => RegistrationUrl is not null;

    /// <summary>
    /// True when winner(s) have been recorded.
    /// </summary>
    public bool HasWinners => Winners.Count > 0;

    /// <summary>
    /// True when a host bowling center is assigned.
    /// </summary>
    public bool HasHost => BowlingCenterName is not null;

    /// <summary>
    /// True when added money is greater than zero.
    /// </summary>
    public bool HasAddedMoney => AddedMoney is > 0;

    /// <summary>
    /// Sum of all prize money across results.
    /// </summary>
    public decimal TotalPayout => Results.Sum(r => r.PrizeMoney);

    /// <summary>
    /// True when total payout is greater than zero.
    /// </summary>
    public bool HasPayout => TotalPayout > 0;

    /// <summary>
    /// True when at least one sponsor is present.
    /// </summary>
    public bool HasSponsors => Sponsors.Count > 0;

    /// <summary>
    /// True when at least one oil pattern is present.
    /// </summary>
    public bool HasOilPatterns => OilPatterns.Count > 0;

    /// <summary>
    /// True when the tournament spans more than one day.
    /// </summary>
    public bool IsMultiDay => EndDate > StartDate;

    /// <summary>
    /// True when the start date is today or in the future.
    /// </summary>
    public bool IsUpcoming => StartDate >= DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// Results where the bowler competed in the main cut (no side cut).
    /// </summary>
    public IEnumerable<TournamentResultViewModel> MainCutResults =>
        Results.Where(r => r.SideCutName is null);

    /// <summary>
    /// Side cut results grouped into display-ready sections.
    /// </summary>
    public IEnumerable<SideCutGroupViewModel> SideCutGroups =>
        Results
            .Where(r => r.SideCutName is not null)
            .GroupBy(r => r.SideCutName!)
            .Select(g => new SideCutGroupViewModel
            {
                Name = g.Key,
                Indicator = g.First().SideCutIndicator,
                Results = [.. g],
            });

    /// <summary>
    /// Combined bowling center name, city, and state for display; null when host is unknown.
    /// </summary>
    public string? DisplayLocation
    {
        get
        {
            if (BowlingCenterName is null)
            {
                return null;
            }

            if (BowlingCenterCity is null)
            {
                return BowlingCenterName;
            }

            return BowlingCenterState is null
                ? $"{BowlingCenterName} · {BowlingCenterCity}"
                : $"{BowlingCenterName} · {BowlingCenterCity}, {BowlingCenterState}";
        }
    }

    /// <summary>
    /// Formats the start/end date range for display.
    /// </summary>
    public string FormatDateRange()
    {
        if (!IsMultiDay)
        {
            return StartDate.ToString("MMM d, yyyy", System.Globalization.CultureInfo.CurrentCulture);
        }

        return StartDate.Month == EndDate.Month
            ? $"{StartDate.ToString("MMM d", System.Globalization.CultureInfo.CurrentCulture)}–{EndDate.ToString("d, yyyy", System.Globalization.CultureInfo.CurrentCulture)}"
            : $"{StartDate.ToString("MMM d", System.Globalization.CultureInfo.CurrentCulture)} – {EndDate.ToString("MMM d, yyyy", System.Globalization.CultureInfo.CurrentCulture)}";
    }
}