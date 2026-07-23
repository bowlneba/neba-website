namespace Neba.Api.Contracts.Tournaments.CreateTournament;

/// <summary>
/// The fields required to create a tournament.
/// </summary>
public sealed record TournamentInput
{
    /// <summary>
    /// The tournament's name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The tournament type name (see <c>TournamentType</c>).
    /// </summary>
    public required string TournamentType { get; init; }

    /// <summary>
    /// The first day of competition.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The last day of competition.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Whether results from this tournament count toward statistics such as Bowler of the Year.
    /// </summary>
    public required bool StatsEligible { get; init; }

    /// <summary>
    /// The entry fee, in dollars.
    /// </summary>
    public required decimal EntryFee { get; init; }

    /// <summary>
    /// The certification number of the bowling center hosting this tournament; null if not yet assigned.
    /// </summary>
    public string? BowlingCenterCertificationNumber { get; init; }

    /// <summary>
    /// The URL bowlers use to register externally, if any.
    /// </summary>
    public Uri? ExternalRegistrationUrl { get; init; }

    /// <summary>
    /// The tournament's logo image, already uploaded to storage.
    /// </summary>
    public TournamentLogoInput? Logo { get; init; }

    /// <summary>
    /// An existing oil pattern's ID. When set, <see cref="PatternLengthCategory"/>
    /// and <see cref="PatternRatioCategory"/> must be null — they're derived from the pattern instead.
    /// </summary>
    public string? OilPatternId { get; init; }

    /// <summary>
    /// Manual pattern length category name (see <c>PatternLengthCategory</c>). Only valid when
    /// <see cref="OilPatternId"/> is null.
    /// </summary>
    public string? PatternLengthCategory { get; init; }

    /// <summary>
    /// Manual pattern ratio category name (see <c>PatternRatioCategory</c>). Only valid when
    /// <see cref="OilPatternId"/> is null.
    /// </summary>
    public string? PatternRatioCategory { get; init; }

    /// <summary>
    /// Date/time at which full oil pattern details become visible to callers without the tournament
    /// management permission; null if there's no reveal restriction. Callers holding the tournament
    /// management permission always see full details.
    /// </summary>
    public DateTimeOffset? OilPatternRevealDateTime { get; init; }
}