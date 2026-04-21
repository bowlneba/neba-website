using System.Globalization;

using ErrorOr;

using Neba.Domain.BowlingCenters;
using Neba.Domain.Seasons;

namespace Neba.Domain.Tournaments;

/// <summary>
/// A NEBA-sanctioned scratch bowling competition consisting of one or more qualifying squads
/// followed by a single-elimination match play championship round to determine a winner.
/// Tournament format, eligibility, and team composition are governed by the associated
/// <see cref="TournamentType"/>.
/// </summary>
public sealed class Tournament
    : AggregateRoot
{
    /// <summary>
    /// Gets the unique identifier for this tournament.
    /// </summary>
    public required TournamentId Id { get; init; }

    /// <summary>
    /// Gets the publicly displayed name of the tournament as it appears in schedules and results.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the format classification of the tournament, which governs team size, eligibility
    /// restrictions, and match play structure.
    /// See <see cref="TournamentType"/> for valid values.
    /// </summary>
    public required TournamentType TournamentType { get; init; }

    /// <summary>
    /// Gets the date on which the first qualifying squad of the tournament is held.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets the date on which the final round of competition concludes.
    /// For single-day tournaments this will equal <see cref="StartDate"/>.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// Gets the USBC certification number of the bowling center where the tournament is held,
    /// or <see langword="null"/> if the venue has not yet been assigned.
    /// </summary>
    public CertificationNumber? BowlingCenterId { get; init; }

    internal BowlingCenter? BowlingCenter { get; init; }

    /// <summary>
    /// Gets the oil-to-dry ratio category of the lane condition used in this tournament,
    /// or <see langword="null"/> if the pattern has not yet been designated.
    /// See <see cref="PatternRatioCategory"/> for valid values.
    /// </summary>
    public PatternRatioCategory? PatternRatioCategory { get; init; }

    /// <summary>
    /// Gets the length category of the oil pattern applied to the lanes for this tournament,
    /// or <see langword="null"/> if the pattern has not yet been designated.
    /// See <see cref="PatternLengthCategory"/> for valid values.
    /// </summary>
    public PatternLengthCategory? PatternLengthCategory { get; init; }

    /// <summary>
    /// Gets the legacy numeric identifier for this tournament, carried over from the previous
    /// system. <see langword="null"/> for tournaments created after the system migration.
    /// </summary>
    public int? LegacyId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the season in which this tournament takes place.
    /// </summary>
    public required SeasonId SeasonId { get; init; }

    internal Season Season { get; init; } = null!;

    private readonly List<TournamentSponsor> _sponsors = [];

    /// <summary>
    /// Gets the collection of sponsors associated with this tournament, along with details about
    /// </summary>
    public IReadOnlyCollection<TournamentSponsor> Sponsors
        => _sponsors;
}

internal static class TournamentErrors
{
    public static Error InvalidTournamentDatesForSeason(DateOnly seasonStartDate, DateOnly seasonEndDate)
    {
        return Error.Validation(
            code: "Tournaments.InvalidDatesForSeason",
            description: "Tournament dates must fall within the season dates.",
            metadata: new Dictionary<string, object>
            {
                { "SeasonStartDate", seasonStartDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture) },
                { "SeasonEndDate", seasonEndDate.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture) },
            });
    }
}