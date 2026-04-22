namespace Neba.Website.Server.Tournaments.Schedule;

/// <summary>Summary of a single tournament for display in the tournament schedule.</summary>
public sealed record TournamentSummaryViewModel
{
    // ── Core ─────────────────────────────────────────────────────────────────

    /// <summary>Unique tournament identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Publicly displayed tournament name.</summary>
    public required string Name { get; init; }

    /// <summary>Season label, e.g. "2026" or "2020-21".</summary>
    public required string Season { get; init; }

    /// <summary>Date the first qualifying squad is held.</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Date the final round concludes; equals StartDate for single-day events.</summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>Format category of the tournament.</summary>
    public required TournamentType TournamentType { get; init; }

    /// <summary>Per-bowler entry fee in dollars, if applicable.</summary>
    public decimal? EntryFee { get; init; }

    /// <summary>Current registration state; null means registration has not opened yet.</summary>
    public RegistrationStatus? RegistrationStatus { get; init; }

    /// <summary>External URL for online registration.</summary>
    public Uri? RegistrationUrl { get; init; }

    // ── Nullable – confirmed closer to event ─────────────────────────────────

    /// <summary>Name of the host bowling center.</summary>
    public string? BowlingCenterName { get; init; }

    /// <summary>City of the host bowling center.</summary>
    public string? BowlingCenterCity { get; init; }

    /// <summary>Primary sponsor name.</summary>
    public string? Sponsor { get; init; }

    /// <summary>URL to the tournament logo image; null when unavailable.</summary>
    public Uri? TournamentLogoUrl { get; init; }

    /// <summary>Total prize money added by the sponsor.</summary>
    public decimal? AddedMoney { get; init; }

    /// <summary>Number of entries received so far.</summary>
    public int? Entries { get; init; }

    /// <summary>Maximum number of entries allowed.</summary>
    public int? MaxEntries { get; init; }

    // ── Pattern ───────────────────────────────────────────────────────────────

    /// <summary>Name of the oil pattern used (public portion only).</summary>
    public string? PatternName { get; init; }

    /// <summary>Pattern length in feet.</summary>
    public int? PatternLength { get; init; }

    /// <summary>Pattern ratio category (public portion only).</summary>
    public string? PatternLengthCategory { get; init; }

    // ── Past only ─────────────────────────────────────────────────────────────

    /// <summary>Names of the winning bowler(s); empty when results are not yet available.</summary>
    public IReadOnlyCollection<string> Winners { get; init; } = [];

    // ── Computed convenience ──────────────────────────────────────────────────

    /// <summary>True when the tournament spans more than one day.</summary>
    public bool IsMultiDay => EndDate > StartDate;

    /// <summary>True when added money is greater than zero.</summary>
    public bool HasAddedMoney => AddedMoney is > 0;

    /// <summary>True when both entry count and cap are known.</summary>
    public bool HasCapacityData => Entries.HasValue && MaxEntries.HasValue;

    /// <summary>True when a host bowling center is assigned.</summary>
    public bool HasHost => BowlingCenterName is not null;

    /// <summary>True when a sponsor is assigned.</summary>
    public bool HasSponsor => Sponsor is not null;

    /// <summary>True when winner(s) have been recorded for this tournament.</summary>
    public bool HasWinners => Winners.Count > 0;

    /// <summary>True when a registration URL is available.</summary>
    public bool CanRegister => RegistrationUrl is not null;

    /// <summary>True when the event end date is before today.</summary>
    public bool IsPast => EndDate < DateOnly.FromDateTime(DateTime.Today);

    /// <summary>True when this entry belongs to the merged 2020-21 COVID season.</summary>
    public bool IsMergedSeason => Season == "2020-21";

    /// <summary>Whole days remaining until start (0 if today or past).</summary>
    public int DaysUntilStart =>
        Math.Max(0, (StartDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days);

    /// <summary>True when start is within 21 days and has not passed.</summary>
    public bool IsUrgent =>
        (StartDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days is >= 0 and <= 21;

    /// <summary>Combined center name and city for display; null when host is unknown.</summary>
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

            return $"{BowlingCenterName} · {BowlingCenterCity}";
        }
    }

    /// <summary>Label for the primary price display ("Added money" or "Entry fee").</summary>
    public string DisplayPriceLabel => HasAddedMoney ? "Added money" : "Entry fee";

    /// <summary>Primary price to display (added money takes precedence over entry fee).</summary>
    public decimal? DisplayPrice => HasAddedMoney ? AddedMoney : EntryFee;

    /// <summary>Formats the start/end date range for display.</summary>
    public string FormatDateRange()
    {
        if (!IsMultiDay)
        {
            return StartDate.ToString("MMM d, yyyy", System.Globalization.CultureInfo.CurrentCulture);
        }

        if (StartDate.Month == EndDate.Month)
        {
            return $"{StartDate.ToString("MMM d", System.Globalization.CultureInfo.CurrentCulture)}–{EndDate.ToString("d, yyyy", System.Globalization.CultureInfo.CurrentCulture)}";
        }

        return $"{StartDate.ToString("MMM d", System.Globalization.CultureInfo.CurrentCulture)} – {EndDate.ToString("MMM d, yyyy", System.Globalization.CultureInfo.CurrentCulture)}";
    }

    /// <summary>
    /// Pattern descriptor for UI chips, preferring name + length and falling back to length category.
    /// </summary>
    public string? PatternDisplay
    {
        get
        {
            return PatternName is not null && PatternLength.HasValue
                ? PatternName + " · " + PatternLength.Value.ToString(System.Globalization.CultureInfo.CurrentCulture) + " ft"
                : PatternLengthCategory;
        }
    }
}